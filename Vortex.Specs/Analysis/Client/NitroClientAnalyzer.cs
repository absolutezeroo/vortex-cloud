using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Vortex.Specs.Model;
using Vortex.Specs.Naming;
using Vortex.Specs.Sources;

namespace Vortex.Specs.Analysis.Client;

/// <summary>
/// Reads the Nitro client's packet definitions.
/// </summary>
/// <remarks>
/// Nitro is a community reimplementation, not Sulake code, so it carries
/// <see cref="EvidenceAuthority.MultiImplementation"/> weight at best — never
/// <see cref="EvidenceAuthority.ClientCode"/>. What makes it worth reading anyway is that it is the
/// only tree in this workspace where every field has a name and a type in machine-readable form; the
/// official client's are obfuscated away. It is therefore the best source of *candidate* names,
/// which the ActionScript reader then corroborates or contradicts for the actual target build.
/// </remarks>
public sealed partial class NitroClientAnalyzer(SpecWorkspace workspace, SourceTree tree)
    : IClientAnalyzer
{
    public string Origin => "nitro";

    private static readonly Dictionary<string, WireType> ReadOps = new(StringComparer.Ordinal)
    {
        ["readInt"] = WireType.Int32,
        ["readString"] = WireType.String,
        ["readBoolean"] = WireType.Boolean,
        ["readShort"] = WireType.Short,
        ["readByte"] = WireType.Byte,
        ["readFloat"] = WireType.Float,
        ["readDouble"] = WireType.Double,
    };

    [GeneratedRegex(@"public\s+static\s+(\w+)\s*=\s*(-?\d+)\s*;", RegexOptions.None, 2000)]
    private static partial Regex HeaderEntry();

    [GeneratedRegex(
        @"export\s+class\s+(\w+)\s+implements\s+I(Outgoing|Incoming)Packet",
        RegexOptions.None,
        2000
    )]
    private static partial Regex PacketClass();

    [GeneratedRegex(@"export\s+type\s+(\w+)\s*=\s*", RegexOptions.None, 2000)]
    private static partial Regex TypeAlias();

    [GeneratedRegex(@"(\w+)\s*\??\s*:\s*([^;\r\n]+);", RegexOptions.None, 2000)]
    private static partial Regex TypeMember();

    [GeneratedRegex(@"wrapper\.(read\w+)\s*\(", RegexOptions.None, 2000)]
    private static partial Regex ReadCall();

    [GeneratedRegex(@"(\w+)\s*\(\s*wrapper\s*[,)]", RegexOptions.None, 2000)]
    private static partial Regex HelperCall();

    [GeneratedRegex(@"this\.params\.(\w+)", RegexOptions.None, 2000)]
    private static partial Regex ComposedField();

    [GeneratedRegex(@"compose\s*\(\s*\)", RegexOptions.None, 2000)]
    private static partial Regex ComposeMethod();

    [GeneratedRegex(@"parse\s*\(\s*wrapper", RegexOptions.None, 2000)]
    private static partial Regex ParseMethod();

    public ClientScan Scan()
    {
        string packets = Path.Combine(tree.Root, "packages", "nitro-shared", "src", "packets");

        if (!Directory.Exists(packets))
        {
            return new ClientScan
            {
                Origin = Origin,
                Authority = EvidenceAuthority.MultiImplementation,
                Packets = [],
                Unresolved = [$"no packets directory under {workspace.Relative(tree.Root)}"],
            };
        }

        Dictionary<string, int> clientOutgoing = ReadHeaderTable(
            Path.Combine(packets, "OutgoingHeader.ts")
        );
        Dictionary<string, int> clientIncoming = ReadHeaderTable(
            Path.Combine(packets, "IncomingHeader.ts")
        );

        List<string> unresolved = [];
        List<ClientPacket> results = [];
        Dictionary<string, string> helpers = IndexHelpers(packets);

        foreach (
            string file in Directory
                .EnumerateFiles(packets, "*.ts", SearchOption.AllDirectories)
                .OrderBy(f => f, StringComparer.Ordinal)
        )
        {
            string text = File.ReadAllText(file);
            string masked = SourceTextScanner.Mask(text);
            Match declaration = PacketClass().Match(masked);

            if (!declaration.Success)
            {
                continue;
            }

            string className = declaration.Groups[1].Value;
            bool clientToServer = declaration.Groups[2].Value == "Outgoing";

            ClientPacket? packet = clientToServer
                ? ReadComposer(file, text, masked, className, clientOutgoing, unresolved)
                : ReadParser(file, text, masked, className, clientIncoming, helpers, unresolved);

            if (packet is not null)
            {
                results.Add(packet);
            }
        }

        HeaderTableFolder.Result incoming = HeaderTableFolder.Fold(clientOutgoing);
        HeaderTableFolder.Result outgoing = HeaderTableFolder.Fold(clientIncoming);
        unresolved.AddRange(incoming.Collisions);
        unresolved.AddRange(outgoing.Collisions);

        return new ClientScan
        {
            Origin = Origin,
            Authority = EvidenceAuthority.MultiImplementation,
            // Nitro's tables are for its own build: MoveObject is 2828 there against 1482 here.
            // Saying so is what stops the conflict detector reporting every id as a disagreement.
            TargetsSameRevision = false,
            Packets = results,
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
            Match match in HeaderEntry().Matches(SourceTextScanner.Mask(File.ReadAllText(path)))
        )
        {
            if (int.TryParse(match.Groups[2].Value, out int id))
            {
                table[match.Groups[1].Value] = id;
            }
        }

        return table;
    }

    /// <summary>Shared field-block readers, e.g. <c>Data/FloorItemParser.ts</c>.</summary>
    private static Dictionary<string, string> IndexHelpers(string packets)
    {
        Dictionary<string, string> helpers = new(StringComparer.Ordinal);

        foreach (
            string file in Directory
                .EnumerateFiles(packets, "*Parser.ts", SearchOption.AllDirectories)
                .OrderBy(f => f, StringComparer.Ordinal)
        )
        {
            helpers[Path.GetFileNameWithoutExtension(file)] = file;
        }

        return helpers;
    }

    private ClientPacket? ReadComposer(
        string file,
        string text,
        string masked,
        string className,
        IReadOnlyDictionary<string, int> headers,
        List<string> unresolved
    )
    {
        Match compose = ComposeMethod().Match(masked);

        if (!compose.Success)
        {
            unresolved.Add($"{className}: no compose() body");
            return null;
        }

        (int Start, int End)? body = SourceTextScanner.BlockAfter(masked, compose.Index);

        if (body is null)
        {
            unresolved.Add($"{className}: unbalanced compose() body");
            return null;
        }

        Dictionary<string, string> declaredTypes = ReadTypeAlias(masked, className + "Type");
        List<ClientField> fields = [];

        foreach (
            Match field in SourceTextScanner.Matches(
                masked,
                ComposedField(),
                body.Value.Start,
                body.Value.End
            )
        )
        {
            string name = field.Groups[1].Value;
            declaredTypes.TryGetValue(name, out string? declared);

            fields.Add(
                new ClientField
                {
                    Name = PacketNaming.SnakeCase(name),
                    Type = MapTypeScriptType(declared),
                    SemanticType = declared is "number" or "string" or "boolean" ? null : declared,
                }
            );
        }

        // A composer that reaches past this.params — a spread, a computed array — is not fully
        // described by the fields above, and saying so keeps it out of "the client has fewer
        // fields" conclusions it cannot support.
        bool partial =
            fields.Count == 0
            || masked[body.Value.Start..body.Value.End].Contains("...", StringComparison.Ordinal);

        return new ClientPacket
        {
            Canonical = PacketNaming.Canonical(className),
            Direction = PacketDirection.Incoming,
            DeclaredType = className,
            HeaderId = headers.TryGetValue(className, out int id) ? id : null,
            Fields = fields,
            IsPartial = partial,
            Evidence = Evidence(
                EvidenceKind.ClientComposer,
                file,
                className,
                SourceTextScanner.LineAt(text, compose.Index)
            ),
        };
    }

    private ClientPacket? ReadParser(
        string file,
        string text,
        string masked,
        string className,
        IReadOnlyDictionary<string, int> headers,
        IReadOnlyDictionary<string, string> helpers,
        List<string> unresolved
    )
    {
        Match parse = ParseMethod().Match(masked);

        if (!parse.Success)
        {
            unresolved.Add($"{className}: no parse() body");
            return null;
        }

        (int Start, int End)? body = SourceTextScanner.BlockAfter(masked, parse.Index);

        if (body is null)
        {
            unresolved.Add($"{className}: unbalanced parse() body");
            return null;
        }

        (List<ClientField> fields, bool partial) = ReadFieldsFrom(
            masked,
            body.Value.Start,
            body.Value.End,
            helpers,
            unresolved,
            depth: 0
        );

        return new ClientPacket
        {
            Canonical = PacketNaming.Canonical(className),
            Direction = PacketDirection.Outgoing,
            DeclaredType = className,
            HeaderId = headers.TryGetValue(className, out int id) ? id : null,
            Fields = fields,
            IsPartial = partial,
            Evidence = Evidence(
                EvidenceKind.ClientParser,
                file,
                className,
                SourceTextScanner.LineAt(text, parse.Index)
            ),
        };
    }

    /// <summary>
    /// Reads every wire access in a parse body in source order, naming each one from the assignment
    /// it lands in and expanding calls into shared field-block readers.
    /// </summary>
    private (List<ClientField> Fields, bool Partial) ReadFieldsFrom(
        string masked,
        int start,
        int end,
        IReadOnlyDictionary<string, string> helpers,
        List<string> unresolved,
        int depth
    )
    {
        List<ClientField> fields = [];
        bool partial = false;

        List<(int Index, ClientField Field)> found = [];

        foreach (Match read in SourceTextScanner.Matches(masked, ReadCall(), start, end))
        {
            if (!ReadOps.TryGetValue(read.Groups[1].Value, out WireType type))
            {
                partial = true;
                unresolved.Add($"unmapped read primitive {read.Groups[1].Value}");
                continue;
            }

            bool inLoop = SourceTextScanner.DepthAt(masked, start, read.Index) > 0;

            found.Add(
                (
                    read.Index,
                    new ClientField
                    {
                        Name = NameBefore(masked, start, read.Index),
                        Type = type,
                        Note = inLoop ? "inside a repeated block" : null,
                    }
                )
            );
        }

        foreach (Match helper in SourceTextScanner.Matches(masked, HelperCall(), start, end))
        {
            string name = helper.Groups[1].Value;

            if (!helpers.TryGetValue(name, out string? helperFile))
            {
                if (name is not ("parse" or "readBytes"))
                {
                    partial = true;
                    unresolved.Add($"unresolved field-block reader {name}");
                }

                continue;
            }

            if (depth >= 3)
            {
                partial = true;
                unresolved.Add($"{name}: deeper than the block-expansion limit");
                continue;
            }

            string helperText = SourceTextScanner.Mask(File.ReadAllText(helperFile));
            (List<ClientField> children, bool childPartial) = ReadFieldsFrom(
                helperText,
                0,
                helperText.Length,
                helpers,
                unresolved,
                depth + 1
            );

            partial |= childPartial;

            found.Add(
                (
                    helper.Index,
                    new ClientField
                    {
                        Name = PacketNaming.SnakeCase(
                            name.Replace("Parser", string.Empty, StringComparison.Ordinal)
                        ),
                        Type = WireType.Block,
                        Children = children,
                    }
                )
            );
        }

        foreach ((int _, ClientField field) in found.OrderBy(f => f.Index))
        {
            fields.Add(field);
        }

        return (fields, partial || fields.Count == 0);
    }

    /// <summary>
    /// The name a read lands in: the <c>key:</c> of an object-literal entry or the identifier of a
    /// <c>const</c>/<c>let</c> declaration on the same line.
    /// </summary>
    private static string? NameBefore(string masked, int start, int index)
    {
        int lineStart = masked.LastIndexOf('\n', Math.Max(index - 1, 0));
        lineStart = lineStart < start ? start : lineStart + 1;

        if (lineStart >= index)
        {
            return null;
        }

        string line = masked[lineStart..index];
        int colon = line.LastIndexOf(':');

        if (colon > 0)
        {
            string candidate = line[..colon].Trim();

            return IsIdentifier(candidate) ? PacketNaming.SnakeCase(candidate) : null;
        }

        int assign = line.LastIndexOf('=');

        if (assign > 0)
        {
            string[] parts = line[..assign]
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 0 && IsIdentifier(parts[^1]))
            {
                return PacketNaming.SnakeCase(parts[^1]);
            }
        }

        return null;
    }

    private static bool IsIdentifier(string candidate)
    {
        if (candidate.Length == 0 || (!char.IsLetter(candidate[0]) && candidate[0] != '_'))
        {
            return false;
        }

        foreach (char c in candidate)
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
            {
                return false;
            }
        }

        return true;
    }

    private static Dictionary<string, string> ReadTypeAlias(string masked, string aliasName)
    {
        Dictionary<string, string> members = new(StringComparer.Ordinal);

        foreach (Match alias in TypeAlias().Matches(masked))
        {
            if (alias.Groups[1].Value != aliasName)
            {
                continue;
            }

            (int Start, int End)? block = SourceTextScanner.BlockAfter(masked, alias.Index);

            if (block is null)
            {
                return members;
            }

            foreach (
                Match member in SourceTextScanner.Matches(
                    masked,
                    TypeMember(),
                    block.Value.Start,
                    block.Value.End
                )
            )
            {
                members[member.Groups[1].Value] = member.Groups[2].Value.Trim();
            }

            return members;
        }

        return members;
    }

    private static WireType MapTypeScriptType(string? declared) =>
        declared switch
        {
            "number" => WireType.Int32,
            "string" => WireType.String,
            "boolean" => WireType.Boolean,
            null => WireType.Unknown,
            // An enum alias is carried as an int; anything else is a shape this reader does not model.
            _ when declared.EndsWith("Enum", StringComparison.Ordinal) => WireType.Int32,
            _ => WireType.Unknown,
        };

    private EvidenceRef Evidence(EvidenceKind kind, string file, string symbol, int line) =>
        new()
        {
            Kind = kind,
            Authority = EvidenceAuthority.MultiImplementation,
            Origin = Origin,
            Source = workspace.Relative(file),
            Symbol = symbol,
            Line = line,
            Note = "community client reimplementation; corroborating, never authoritative",
        };
}
