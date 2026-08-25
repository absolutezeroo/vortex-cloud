using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Vortex.Specs.Analysis.Client;
using Vortex.Specs.Model;
using Vortex.Specs.Naming;
using Vortex.Specs.Sources;

namespace Vortex.Specs.Analysis.Reference;

/// <summary>
/// Reads the Arcturus reference emulator: what it parses, what it checks, and what it sends back.
/// </summary>
/// <remarks>
/// A second implementation is useful precisely where the client cannot help — the client shows what
/// a packet looks like, never what the server does with a bad one. Arcturus has had that question
/// answered by years of players hitting it, which makes it good evidence and still not authority:
/// it is a reimplementation, its authors guessed too, and where it disagrees with this emulator the
/// output is a recorded conflict rather than a correction.
/// </remarks>
public sealed partial class ArcturusReferenceAnalyzer(SpecWorkspace workspace, SourceTree tree)
    : IReferenceAnalyzer
{
    public string Origin => tree.Id;

    private static readonly Dictionary<string, WireType> ReadOps = new(StringComparer.Ordinal)
    {
        ["readInt"] = WireType.Int32,
        ["readString"] = WireType.String,
        ["readBoolean"] = WireType.Boolean,
        ["readShort"] = WireType.Short,
        ["readByte"] = WireType.Byte,
        ["readLong"] = WireType.Long,
    };

    private static readonly Dictionary<string, WireType> WriteOps = new(StringComparer.Ordinal)
    {
        ["appendInt"] = WireType.Int32,
        ["appendString"] = WireType.String,
        ["appendBoolean"] = WireType.Boolean,
        ["appendShort"] = WireType.Short,
        ["appendByte"] = WireType.Byte,
        ["appendDouble"] = WireType.Double,
        ["appendLong"] = WireType.Long,
    };

    [GeneratedRegex(
        @"(?:public|private)\s+(?:static\s+)?final\s+(?:static\s+)?int\s+(\w+)\s*=\s*(-?\d+)\s*;",
        RegexOptions.None,
        2000
    )]
    private static partial Regex HeaderConstant();

    [GeneratedRegex(@"public\s+class\s+(\w+)\s+extends\s+MessageHandler", RegexOptions.None, 2000)]
    private static partial Regex HandlerClass();

    [GeneratedRegex(@"public\s+class\s+(\w+)\s+extends\s+MessageComposer", RegexOptions.None, 2000)]
    private static partial Regex ComposerClass();

    [GeneratedRegex(@"(?:this\.)?packet\.(read\w+)\s*\(", RegexOptions.None, 2000)]
    private static partial Regex ReadCall();

    [GeneratedRegex(@"(?:this\.)?response\.(append\w+)\s*\(", RegexOptions.None, 2000)]
    private static partial Regex WriteCall();

    [GeneratedRegex(
        @"(\w+)\s*\.\s*(sendResponse|sendComposer|sendMessage)\s*\(\s*new\s+(\w+)",
        RegexOptions.None,
        2000
    )]
    private static partial Regex SendCall();

    [GeneratedRegex(@"if\s*\(", RegexOptions.None, 2000)]
    private static partial Regex IfStatement();

    public ReferenceScan Scan()
    {
        string root = Path.Combine(
            tree.Root,
            "src",
            "main",
            "java",
            "com",
            "eu",
            "habbo",
            "messages"
        );

        if (!Directory.Exists(root))
        {
            return new ReferenceScan
            {
                Origin = Origin,
                Authority = EvidenceAuthority.ReferenceEmulator,
                Behaviours = [],
                Composers = [],
                Unresolved = [$"no messages tree under {workspace.Relative(tree.Root)}"],
            };
        }

        List<string> unresolved = [];
        Dictionary<string, int> incomingHeaders = ReadHeaderTable(
            Path.Combine(root, "incoming", "Incoming.java")
        );
        Dictionary<string, int> outgoingHeaders = ReadHeaderTable(
            Path.Combine(root, "outgoing", "Outgoing.java")
        );

        List<ReferenceBehaviour> behaviours = [];
        List<ReferenceComposerLayout> composers = [];

        foreach (
            string file in Directory
                .EnumerateFiles(root, "*.java", SearchOption.AllDirectories)
                .OrderBy(f => f, StringComparer.Ordinal)
        )
        {
            string text = File.ReadAllText(file);
            string masked = SourceTextScanner.Mask(text);

            Match handler = HandlerClass().Match(masked);

            if (handler.Success)
            {
                ReferenceBehaviour? behaviour = ReadHandler(
                    file,
                    text,
                    masked,
                    handler.Groups[1].Value
                );

                if (behaviour is not null)
                {
                    behaviours.Add(behaviour);
                }

                continue;
            }

            Match composer = ComposerClass().Match(masked);

            if (composer.Success)
            {
                ReferenceComposerLayout? layout = ReadComposer(
                    file,
                    text,
                    masked,
                    composer.Groups[1].Value
                );

                if (layout is not null)
                {
                    composers.Add(layout);
                }
            }
        }

        if (behaviours.Count == 0)
        {
            unresolved.Add("no MessageHandler subclasses found; the tree layout may have changed");
        }

        HeaderTableFolder.Result incoming = HeaderTableFolder.Fold(incomingHeaders);
        HeaderTableFolder.Result outgoing = HeaderTableFolder.Fold(outgoingHeaders);
        unresolved.AddRange(incoming.Collisions);
        unresolved.AddRange(outgoing.Collisions);

        return new ReferenceScan
        {
            Origin = Origin,
            Authority = EvidenceAuthority.ReferenceEmulator,
            Behaviours = behaviours,
            Composers = composers,
            IncomingHeaders = incoming.Table,
            OutgoingHeaders = outgoing.Table,
            Unresolved =
            [
                .. unresolved
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(u => u, StringComparer.Ordinal),
            ],
        };
    }

    private static Dictionary<string, int> ReadHeaderTable(string path)
    {
        Dictionary<string, int> table = new(StringComparer.Ordinal);

        if (!File.Exists(path))
        {
            return table;
        }

        foreach (
            Match match in HeaderConstant().Matches(SourceTextScanner.Mask(File.ReadAllText(path)))
        )
        {
            if (int.TryParse(match.Groups[2].Value, out int id))
            {
                table[match.Groups[1].Value] = id;
            }
        }

        return table;
    }

    private ReferenceBehaviour? ReadHandler(
        string file,
        string text,
        string masked,
        string className
    )
    {
        int handle = masked.IndexOf("void handle()", StringComparison.Ordinal);

        if (handle < 0)
        {
            return null;
        }

        (int Start, int End)? body = SourceTextScanner.BlockAfter(masked, handle);

        if (body is null)
        {
            return null;
        }

        EvidenceRef evidence = Evidence(
            EvidenceKind.ReferenceHandler,
            file,
            className,
            SourceTextScanner.LineAt(text, handle)
        );

        List<ClientField> fields = [];

        foreach (
            Match read in SourceTextScanner.Matches(
                masked,
                ReadCall(),
                body.Value.Start,
                body.Value.End
            )
        )
        {
            if (ReadOps.TryGetValue(read.Groups[1].Value, out WireType type))
            {
                fields.Add(
                    new ClientField
                    {
                        Name = NameFromAssignment(masked, body.Value.Start, read.Index),
                        Type = type,
                    }
                );
            }
        }

        List<FeatureOutgoing> outgoing = [];
        int order = 0;

        foreach (
            Match send in SourceTextScanner.Matches(
                masked,
                SendCall(),
                body.Value.Start,
                body.Value.End
            )
        )
        {
            outgoing.Add(
                new FeatureOutgoing
                {
                    Packet = PacketNaming.Canonical(send.Groups[3].Value),
                    Recipient = RecipientOf(send.Groups[1].Value, send.Groups[2].Value),
                    RecipientConfidence = Confidence.ReferenceObserved,
                    // Source order inside one handler is the order this implementation emits them.
                    // Whether the official server uses the same order is a separate question that
                    // only a capture answers.
                    Order = order++,
                    Evidence = evidence,
                }
            );
        }

        List<FeatureCheck> checks = [];

        foreach (
            Match branch in SourceTextScanner.Matches(
                masked,
                IfStatement(),
                body.Value.Start,
                body.Value.End
            )
        )
        {
            string? condition = ConditionAt(masked, branch.Index);

            if (condition is null || !GuardReturns(masked, branch.Index))
            {
                continue;
            }

            checks.Add(
                new FeatureCheck
                {
                    Expression = SourceTextScanner.Flatten(condition),
                    OnFail = "return",
                    Evidence = evidence,
                }
            );
        }

        return new ReferenceBehaviour
        {
            Canonical = PacketNaming.Canonical(className),
            HandlerType = className,
            Fields = fields,
            Checks = checks,
            Outgoing = outgoing,
            Evidence = evidence,
        };
    }

    private ReferenceComposerLayout? ReadComposer(
        string file,
        string text,
        string masked,
        string className
    )
    {
        int compose = masked.IndexOf("composeInternal()", StringComparison.Ordinal);

        if (compose < 0)
        {
            return null;
        }

        (int Start, int End)? body = SourceTextScanner.BlockAfter(masked, compose);

        if (body is null)
        {
            return null;
        }

        List<ClientField> fields = [];

        foreach (
            Match write in SourceTextScanner.Matches(
                masked,
                WriteCall(),
                body.Value.Start,
                body.Value.End
            )
        )
        {
            if (WriteOps.TryGetValue(write.Groups[1].Value, out WireType type))
            {
                fields.Add(new ClientField { Name = null, Type = type });
            }
        }

        // Arcturus factors shared blocks into serialize* helpers on the domain objects. Those bytes
        // are real and this reader does not follow them, so the layout is explicitly partial rather
        // than quietly short.
        bool partial = masked[body.Value.Start..body.Value.End]
            .Contains("serialize", StringComparison.OrdinalIgnoreCase);

        return new ReferenceComposerLayout
        {
            Canonical = PacketNaming.Canonical(className),
            ComposerType = className,
            Fields = fields,
            IsPartial = partial || fields.Count == 0,
            Evidence = Evidence(
                EvidenceKind.ReferenceComposer,
                file,
                className,
                SourceTextScanner.LineAt(text, compose)
            ),
        };
    }

    private static Recipient RecipientOf(string receiver, string method) =>
        (receiver, method) switch
        {
            ("client", "sendResponse") => Recipient.Actor,
            ("room", "sendComposer") => Recipient.RoomUsers,
            (_, "sendComposer") => Recipient.RoomUsers,
            (_, "sendResponse") => Recipient.TargetUser,
            _ => Recipient.Unknown,
        };

    private static string? NameFromAssignment(string masked, int start, int index)
    {
        int lineStart = masked.LastIndexOf('\n', Math.Max(index - 1, 0));
        lineStart = lineStart < start ? start : lineStart + 1;

        if (lineStart >= index)
        {
            return null;
        }

        string line = masked[lineStart..index];
        int assign = line.LastIndexOf('=');

        if (assign <= 0)
        {
            return null;
        }

        string[] parts = line[..assign].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        return parts.Length > 0 ? PacketNaming.SnakeCase(parts[^1]) : null;
    }

    private static string? ConditionAt(string masked, int ifIndex)
    {
        int open = masked.IndexOf('(', ifIndex);

        if (open < 0)
        {
            return null;
        }

        int depth = 0;

        for (int i = open; i < masked.Length; i++)
        {
            if (masked[i] == '(')
            {
                depth++;
            }
            else if (masked[i] == ')')
            {
                depth--;

                if (depth == 0)
                {
                    return masked[(open + 1)..i];
                }
            }
        }

        return null;
    }

    /// <summary>
    /// True when the branch is an early exit rather than the happy path. Arcturus writes these
    /// without braces as often as with them, so both shapes are checked.
    /// </summary>
    private static bool GuardReturns(string masked, int ifIndex)
    {
        int close = masked.IndexOf(')', ifIndex);

        if (close < 0)
        {
            return false;
        }

        int lookahead = Math.Min(close + 120, masked.Length);
        string following = masked[close..lookahead];
        int brace = following.IndexOf('{', StringComparison.Ordinal);
        int returns = following.IndexOf("return", StringComparison.Ordinal);

        if (returns < 0)
        {
            return false;
        }

        return brace < 0 || returns > brace;
    }

    private EvidenceRef Evidence(EvidenceKind kind, string file, string symbol, int line) =>
        new()
        {
            Kind = kind,
            Authority = EvidenceAuthority.ReferenceEmulator,
            Origin = Origin,
            Source = workspace.Relative(file),
            Symbol = symbol,
            Line = line,
            Note = "third-party server implementation; evidence, not authority",
        };
}
