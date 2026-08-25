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
/// Reads the decompiled official Flash client.
/// </summary>
/// <remarks>
/// This is the only tree in the workspace that is Sulake's own code for the build this emulator
/// targets, so it is the highest-authority structural source available short of a capture.
/// Identifiers are obfuscated, but two things survive and are what make it worth reading:
/// <list type="bullet">
/// <item>the message registry binds each real header id to a class, and those ids are for the target
/// build — which is what lets a class with a meaningless name be joined to a symbolic one;</item>
/// <item>public getters on parsers and named private fields on composers escaped renaming often
/// enough to name a large share of the fields, in the client's own vocabulary.</item>
/// </list>
/// A class whose name did not survive is reported under its obfuscated name with the header id
/// attached; naming it is the resolver's job, not this reader's.
/// </remarks>
/// <param name="targetRevision">
/// The build this emulator speaks. A client tree for any other build still contributes field names
/// and layouts — Habbo's own class names and field shapes barely move between builds — but its
/// header ids belong to that build and are never joined or compared against this one.
/// </param>
public sealed partial class As3ClientAnalyzer(
    SpecWorkspace workspace,
    SourceTree tree,
    string targetRevision
) : IClientAnalyzer
{
    public string Origin => $"as3:{tree.Revision ?? Path.GetFileName(tree.Root)}";

    private bool IsTargetBuild =>
        tree.Revision is not null
        && string.Equals(tree.Revision, targetRevision, StringComparison.Ordinal);

    private static readonly Dictionary<string, WireType> ReadOps = new(StringComparer.Ordinal)
    {
        ["readInteger"] = WireType.Int32,
        ["readString"] = WireType.String,
        ["readBoolean"] = WireType.Boolean,
        ["readShort"] = WireType.Short,
        ["readLong"] = WireType.Long,
        ["readByte"] = WireType.Byte,
        ["readDouble"] = WireType.Double,
    };

    [GeneratedRegex(@"(\w+)\s*\[\s*(\d+)\s*\]\s*=\s*(\w+)\s*;", RegexOptions.None, 2000)]
    private static partial Regex RegistryEntry();

    [GeneratedRegex(
        @"public\s+class\s+(\w+)\s+implements\s+IMessage(Parser|Composer)",
        RegexOptions.None,
        2000
    )]
    private static partial Regex ClassDeclaration();

    [GeneratedRegex(
        @"public\s+function\s+get\s+(\w+)\s*\(\s*\)\s*:\s*(\w+)\s*\{\s*return\s+(\w+)\s*;",
        RegexOptions.Singleline,
        2000
    )]
    private static partial Regex Getter();

    [GeneratedRegex(@"public\s+function\s+parse\s*\(\s*(\w+)\s*:", RegexOptions.None, 2000)]
    private static partial Regex ParseMethod();

    [GeneratedRegex(@"public\s+class\s+(\w+)\s+extends\s+MessageEvent", RegexOptions.None, 2000)]
    private static partial Regex EventDeclaration();

    /// <summary>
    /// A <c>MessageEvent</c> subclass hands its parser class to the base constructor:
    /// <c>super(param1, _SafeCls_3521)</c>. That second argument is the only link between a header id
    /// and the class that actually reads the bytes.
    /// </summary>
    [GeneratedRegex(@"super\s*\(\s*[^,()]+,\s*(\w+)\s*\)", RegexOptions.None, 2000)]
    private static partial Regex EventSuperCall();

    /// <summary>
    /// A parser handing the wrapper to a helper's constructor: <c>new AreaHideMessageData(param1)</c>.
    /// Those reads are the packet's too, and nothing in the parse method itself shows them.
    /// </summary>
    [GeneratedRegex(@"new\s+(\w+)\s*\(\s*(\w+)\s*\)", RegexOptions.None, 2000)]
    private static partial Regex DelegatedRead();

    /// <summary>The helper's own constructor — the one that takes the wrapper and reads from it.</summary>
    [GeneratedRegex(
        @"function\s+\w+\s*\(\s*\w+\s*:\s*IMessageDataWrapper",
        RegexOptions.None,
        2000
    )]
    private static partial Regex WrapperConstructor();

    [GeneratedRegex(@"public\s+function\s+getMessageArray\s*\(\s*\)", RegexOptions.None, 2000)]
    private static partial Regex MessageArrayMethod();

    [GeneratedRegex(@"\.\s*(read\w+)\s*\(", RegexOptions.None, 2000)]
    private static partial Regex ReadCall();

    [GeneratedRegex(@"(?:this\.)?(\w+)\s*=\s*(param\d+)\s*;", RegexOptions.None, 2000)]
    private static partial Regex ConstructorAssignment();

    [GeneratedRegex(
        @"(?:private|protected|public)\s+var\s+(\w+)\s*:\s*([\w.]+)",
        RegexOptions.None,
        2000
    )]
    private static partial Regex FieldDeclaration();

    [GeneratedRegex(@"(param\d+)\s*:\s*([\w.]+)", RegexOptions.None, 2000)]
    private static partial Regex ParameterDeclaration();

    [GeneratedRegex(@"new\s+(\w+)\s*\(", RegexOptions.None, 2000)]
    private static partial Regex Construction();

    public ClientScan Scan()
    {
        string source = Path.Combine(tree.Root, "src");

        if (!Directory.Exists(source))
        {
            return new ClientScan
            {
                Origin = Origin,
                Authority = EvidenceAuthority.ClientCode,
                Revision = tree.Revision,
                Packets = [],
                Unresolved = [$"no src directory under {workspace.Relative(tree.Root)}"],
            };
        }

        List<string> unresolved = [];
        (Dictionary<string, int> toServer, Dictionary<string, int> toClient) = ReadRegistry(
            source,
            unresolved
        );

        Dictionary<string, string> classFiles = IndexClassFiles(source);

        BindParsersBehindEvents(classFiles, toClient, unresolved);

        List<ClientPacket> packets = [];
        Dictionary<string, CallSite> callSites = new(StringComparer.Ordinal);

        foreach (
            string file in Directory
                .EnumerateFiles(source, "*.as", SearchOption.AllDirectories)
                .OrderBy(f => f, StringComparer.Ordinal)
        )
        {
            string text = File.ReadAllText(file);
            string masked = SourceTextScanner.Mask(text);
            Match declaration = ClassDeclaration().Match(masked);
            string? className = declaration.Success ? declaration.Groups[1].Value : null;

            CollectCallSites(file, text, masked, className, callSites);

            if (className is null)
            {
                continue;
            }

            ClientPacket? packet =
                declaration.Groups[2].Value == "Parser"
                    ? ReadParser(file, text, masked, className, toClient, classFiles)
                    : ReadComposer(file, text, masked, className, toServer);

            if (packet is not null)
            {
                packets.Add(packet);
            }
        }

        return new ClientScan
        {
            Origin = Origin,
            Authority = EvidenceAuthority.ClientCode,
            Revision = tree.Revision,
            TargetsSameRevision = IsTargetBuild,
            Packets = [.. packets.Select(p => WithCallSiteEvidence(p, callSites))],
            // Only the classes whose names survived obfuscation can go in a name-keyed table, which
            // is a small slice of the build — but it is the slice where a wrong header id here can
            // actually be caught, so it is worth publishing.
            IncomingHeaders = NamedHeaders(packets, PacketDirection.Incoming, unresolved),
            OutgoingHeaders = NamedHeaders(packets, PacketDirection.Outgoing, unresolved),
            Unresolved =
            [
                .. unresolved
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(u => u, StringComparer.Ordinal),
            ],
        };
    }

    private static Dictionary<string, int> NamedHeaders(
        IEnumerable<ClientPacket> packets,
        PacketDirection direction,
        List<string> unresolved
    )
    {
        HeaderTableFolder.Result folded = HeaderTableFolder.Fold(
            packets
                .Where(p =>
                    p.Direction == direction
                    && p.HeaderId is not null
                    && !PacketNaming.IsSyntheticTypeName(p.DeclaredType)
                )
                .Select(p => new KeyValuePair<string, int>(p.DeclaredType, p.HeaderId!.Value))
        );

        unresolved.AddRange(folded.Collisions);

        return new Dictionary<string, int>(folded.Table, StringComparer.Ordinal);
    }

    /// <summary>Where a composer is constructed, and how many arguments were passed.</summary>
    private sealed record CallSite(int Count, string File, int Line, int Arity);

    /// <summary>
    /// Records every <c>new SomeComposer(...)</c> in the client tree.
    /// </summary>
    /// <remarks>
    /// The point is not the arity, useful as that is for corroborating a field count. It is that a
    /// composer the client constructs somewhere is a message the client really sends, which is a
    /// stronger claim than "this class exists in the build": a class nothing constructs may be dead
    /// code left over from a feature that shipped and was withdrawn.
    /// </remarks>
    private static void CollectCallSites(
        string file,
        string text,
        string masked,
        string? declaringClass,
        Dictionary<string, CallSite> sink
    )
    {
        foreach (Match construction in Construction().Matches(masked))
        {
            string name = construction.Groups[1].Value;

            // A class constructing itself is not a call site; nor is the composer's own file, where
            // the only mention is its declaration.
            if (string.Equals(name, declaringClass, StringComparison.Ordinal))
            {
                continue;
            }

            int arity = ArgumentCount(masked, construction.Index + construction.Length - 1);

            sink[name] = sink.TryGetValue(name, out CallSite? existing)
                ? existing with
                {
                    Count = existing.Count + 1,
                }
                : new CallSite(1, file, SourceTextScanner.LineAt(text, construction.Index), arity);
        }
    }

    private static int ArgumentCount(string masked, int openParen)
    {
        int depth = 0;
        int count = 0;
        bool sawContent = false;

        for (int i = openParen; i < masked.Length; i++)
        {
            switch (masked[i])
            {
                case '(' or '[' or '{':
                    depth++;
                    break;

                case ')'
                or ']'
                or '}':
                    depth--;

                    if (depth == 0)
                    {
                        return sawContent ? count + 1 : 0;
                    }

                    break;

                case ',' when depth == 1:
                    count++;
                    break;

                default:
                    if (depth == 1 && !char.IsWhiteSpace(masked[i]))
                    {
                        sawContent = true;
                    }

                    break;
            }
        }

        return 0;
    }

    private ClientPacket WithCallSiteEvidence(
        ClientPacket packet,
        IReadOnlyDictionary<string, CallSite> callSites
    )
    {
        if (
            packet.Direction != PacketDirection.Incoming
            || !callSites.TryGetValue(packet.DeclaredType, out CallSite? site)
        )
        {
            return packet;
        }

        // A message the client is written to send is behaviour the server has to support, which is
        // one rung above "the class is present in the build".
        return packet with
        {
            Evidence = packet.Evidence with
            {
                Kind = EvidenceKind.ClientCallSite,
                Authority = EvidenceAuthority.ClientMandated,
                Note = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "the client constructs this composer at {0} call site(s), first at {1}:{2} with "
                        + "{3} argument(s){4}",
                    site.Count,
                    workspace.Relative(site.File),
                    site.Line,
                    site.Arity,
                    site.Arity == packet.Fields.Count
                        ? string.Empty
                        : $"; the class writes {packet.Fields.Count} value(s) to the wire"
                ),
            },
        };
    }

    /// <summary>
    /// Reads the client's own message registry: two arrays of <c>[header] = Class</c>, one per
    /// direction. The array holding the composers is named in the clear; the other one's name is
    /// obfuscated and differs per build, so it is identified by being the other array of the same
    /// shape rather than by a name baked in here.
    /// </summary>
    private (Dictionary<string, int> ToServer, Dictionary<string, int> ToClient) ReadRegistry(
        string source,
        List<string> unresolved
    )
    {
        Dictionary<string, int> toServer = new(StringComparer.Ordinal);
        Dictionary<string, int> toClient = new(StringComparer.Ordinal);

        string? registryFile = Directory
            .EnumerateFiles(source, "*.as", SearchOption.AllDirectories)
            .Where(f => f.Replace('\\', '/').Contains("/communication/", StringComparison.Ordinal))
            .OrderBy(f => f, StringComparer.Ordinal)
            .FirstOrDefault(f =>
                File.ReadAllText(f)
                    .Contains("implements IMessageConfiguration", StringComparison.Ordinal)
            );

        if (registryFile is null)
        {
            unresolved.Add("no IMessageConfiguration registry found in the client tree");
            return (toServer, toClient);
        }

        string masked = SourceTextScanner.Mask(File.ReadAllText(registryFile));
        Dictionary<string, Dictionary<string, int>> byArray = new(StringComparer.Ordinal);

        foreach (Match entry in RegistryEntry().Matches(masked))
        {
            string array = entry.Groups[1].Value;

            if (!int.TryParse(entry.Groups[2].Value, out int header))
            {
                continue;
            }

            if (!byArray.TryGetValue(array, out Dictionary<string, int>? table))
            {
                table = new Dictionary<string, int>(StringComparer.Ordinal);
                byArray[array] = table;
            }

            table[entry.Groups[3].Value] = header;
        }

        foreach (KeyValuePair<string, Dictionary<string, int>> pair in byArray)
        {
            if (pair.Key.Contains("composer", StringComparison.OrdinalIgnoreCase))
            {
                foreach (KeyValuePair<string, int> item in pair.Value)
                {
                    toServer[item.Key] = item.Value;
                }
            }
            else
            {
                foreach (KeyValuePair<string, int> item in pair.Value)
                {
                    toClient[item.Key] = item.Value;
                }
            }
        }

        if (toServer.Count == 0 || toClient.Count == 0)
        {
            unresolved.Add(
                $"registry {workspace.Relative(registryFile)} yielded {toServer.Count} composer and "
                    + $"{toClient.Count} event bindings; one side is missing"
            );
        }

        return (toServer, toClient);
    }

    /// <summary>
    /// Re-keys the incoming table from event classes onto the parser classes behind them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The client's registry binds a header to a <c>MessageEvent</c> subclass, not to a parser:
    /// <c>_SafeStr_4546[1093] = _SafeCls_3980</c>, and <c>_SafeCls_3980</c> is a four-line wrapper
    /// whose constructor names the real reader. <see cref="ReadParser" /> looks the header up by the
    /// name of the class implementing <c>IMessageParser</c>, which is never the name in the table, so
    /// on an obfuscated build every parser came back with no header and could not be matched to a
    /// packet at all.
    /// </para>
    /// <para>
    /// The cost of that was quiet and large: 17 of 805 outgoing specs carried evidence from the
    /// client this emulator actually targets, against 533 of 604 incoming. Every outgoing comparison
    /// was therefore made against a client from 2016 — which is where the baselined "wire conflicts"
    /// came from. They were not disagreements with the client; they were disagreements with a
    /// different client.
    /// </para>
    /// <para>
    /// The original entry is left in place. A wrapper class implements no parser interface, so it can
    /// never be looked up, and removing it would only make the table harder to read against the
    /// registry it came from.
    /// </para>
    /// </remarks>
    /// <summary>
    /// Class name to file path. AS3 requires one public class per file, named after it, so this is
    /// the file listing and costs no reads — which is what makes following a reference cheap enough
    /// to do twice: once for the parser behind an event, once for a helper a parser delegates to.
    /// </summary>
    private static Dictionary<string, string> IndexClassFiles(string source)
    {
        Dictionary<string, string> byClassName = new(StringComparer.Ordinal);

        foreach (
            string file in Directory.EnumerateFiles(source, "*.as", SearchOption.AllDirectories)
        )
        {
            byClassName[Path.GetFileNameWithoutExtension(file)] = file;
        }

        return byClassName;
    }

    private void BindParsersBehindEvents(
        Dictionary<string, string> byClassName,
        Dictionary<string, int> toClient,
        List<string> unresolved
    )
    {
        int bound = 0;

        foreach (KeyValuePair<string, int> entry in toClient.ToArray())
        {
            if (!byClassName.TryGetValue(entry.Key, out string? file))
            {
                continue;
            }

            string masked = SourceTextScanner.Mask(File.ReadAllText(file));

            if (!EventDeclaration().IsMatch(masked))
            {
                continue;
            }

            Match super = EventSuperCall().Match(masked);

            if (!super.Success)
            {
                continue;
            }

            toClient[super.Groups[1].Value] = entry.Value;
            bound++;
        }

        if (bound == 0)
        {
            unresolved.Add(
                "no MessageEvent wrapper resolved to a parser class; outgoing packets will carry no "
                    + "header from this build"
            );
        }
    }

    private ClientPacket? ReadParser(
        string file,
        string text,
        string masked,
        string className,
        IReadOnlyDictionary<string, int> headers,
        IReadOnlyDictionary<string, string> classFiles
    )
    {
        Match parse = ParseMethod().Match(masked);

        if (!parse.Success)
        {
            return null;
        }

        (int Start, int End)? body = SourceTextScanner.BlockAfter(masked, parse.Index);

        if (body is null)
        {
            return null;
        }

        Dictionary<string, string> names = ReadGetterNames(masked);
        List<ClientField> fields = [];
        bool partial = false;
        int index = 0;
        string wrapper = parse.Groups[1].Value;

        // Two things put a value on the wire and they interleave, so both are walked in the order
        // they appear rather than one after the other: a read the parser does itself, and a helper
        // it hands the wrapper to. Taking them separately would reorder the layout, which is the one
        // thing a wire spec must never do.
        List<Match> steps =
        [
            .. SourceTextScanner
                .Matches(masked, ReadCall(), body.Value.Start, body.Value.End)
                .Concat(
                    SourceTextScanner
                        .Matches(masked, DelegatedRead(), body.Value.Start, body.Value.End)
                        .Where(m => m.Groups[2].Value == wrapper)
                )
                .OrderBy(m => m.Index),
        ];

        foreach (Match step in steps)
        {
            bool inLoop = SourceTextScanner.DepthAt(masked, body.Value.Start, step.Index) > 0;

            if (step.Groups.Count > 2 && step.Groups[2].Success)
            {
                // Delegated. Everything the helper's constructor reads belongs here, at this point,
                // and carries this site's loop marker: a helper built inside a `while` is one entry
                // of a repeated block however many values it reads.
                (IReadOnlyList<WireType> delegated, bool complete) = DelegatedFields(
                    step.Groups[1].Value,
                    classFiles
                );

                partial |= !complete;

                foreach (WireType delegatedType in delegated)
                {
                    fields.Add(
                        new ClientField
                        {
                            Name = null,
                            Type = delegatedType,
                            Note = inLoop
                                ? "inside a repeated block"
                                : $"read by {step.Groups[1].Value}",
                        }
                    );

                    index++;
                }

                continue;
            }

            if (!ReadOps.TryGetValue(step.Groups[1].Value, out WireType type))
            {
                partial = true;
                continue;
            }

            string? backingField = AssignmentTargetBefore(masked, body.Value.Start, step.Index);
            string? name =
                backingField is not null && names.TryGetValue(backingField, out string? getter)
                    ? PacketNaming.SnakeCase(getter)
                    : null;

            fields.Add(
                new ClientField
                {
                    Name = name,
                    Type = type,
                    Note = inLoop ? "inside a repeated block" : null,
                }
            );

            index++;
        }

        return new ClientPacket
        {
            Canonical = PacketNaming.Canonical(className),
            Direction = PacketDirection.Outgoing,
            DeclaredType = className,
            HeaderId = headers.TryGetValue(className, out int id) ? id : null,
            Fields = fields,
            IsPartial = partial || index == 0,
            Evidence = Evidence(
                EvidenceKind.ClientParser,
                file,
                className,
                SourceTextScanner.LineAt(text, parse.Index)
            ),
        };
    }

    /// <summary>
    /// What a helper class reads from the wrapper in its constructor, in order.
    /// </summary>
    /// <returns>
    /// The types, and whether the helper was fully understood. A helper that delegates in turn, or
    /// that this scan cannot find at all, comes back incomplete — the layout is then marked partial
    /// rather than published as a claim about how many values cross the wire. Under-counting quietly
    /// is what made `outgoing/AcceptFriendResult` read as one field.
    /// </returns>
    private static (IReadOnlyList<WireType> Fields, bool Complete) DelegatedFields(
        string helperName,
        IReadOnlyDictionary<string, string> classFiles
    )
    {
        if (!classFiles.TryGetValue(helperName, out string? helperFile))
        {
            return ([], false);
        }

        string masked = SourceTextScanner.Mask(File.ReadAllText(helperFile));
        Match ctor = WrapperConstructor().Match(masked);

        if (!ctor.Success)
        {
            return ([], false);
        }

        (int Start, int End)? body = SourceTextScanner.BlockAfter(masked, ctor.Index);

        if (body is null)
        {
            return ([], false);
        }

        List<WireType> fields = [];
        bool complete = true;

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
                fields.Add(type);
                continue;
            }

            complete = false;
        }

        // One level only. A helper that builds another helper is rare and would need the same care
        // about ordering; saying so is better than guessing at it.
        if (DelegatedRead().IsMatch(masked[body.Value.Start..body.Value.End]))
        {
            complete = false;
        }

        return (fields, complete);
    }

    private ClientPacket? ReadComposer(
        string file,
        string text,
        string masked,
        string className,
        IReadOnlyDictionary<string, int> headers
    )
    {
        Match array = MessageArrayMethod().Match(masked);

        if (!array.Success)
        {
            return null;
        }

        (int Start, int End)? body = SourceTextScanner.BlockAfter(masked, array.Index);

        if (body is null)
        {
            return null;
        }

        Dictionary<string, string> parameterTypes = ReadConstructorParameterTypes(
            masked,
            className
        );
        Dictionary<string, string> fieldToParameter = ReadConstructorAssignments(masked, className);
        Dictionary<string, string> fieldTypes = ReadFieldTypes(masked);

        string returned = masked[body.Value.Start..body.Value.End];
        List<ClientField> fields = [];
        bool partial = true;

        int open = returned.IndexOf('[', StringComparison.Ordinal);
        int close = returned.LastIndexOf(']');

        if (open >= 0 && close > open)
        {
            partial = false;
            string inside = returned[(open + 1)..close].Trim();

            if (inside.Length > 0)
            {
                foreach (string element in SplitTopLevel(inside))
                {
                    string expression = element.Trim();
                    string? member = LastIdentifier(expression);

                    // The backing field's own declaration is the most direct statement of the type
                    // and survives obfuscation intact. The constructor-parameter route is the
                    // fallback for composers that pass a value straight through without storing it.
                    string? declared =
                        member is not null && fieldTypes.TryGetValue(member, out string? fieldType)
                            ? fieldType
                            : null;

                    if (declared is null)
                    {
                        string? parameter =
                            member is not null
                            && fieldToParameter.TryGetValue(member, out string? mapped)
                                ? mapped
                                : member;

                        declared =
                            parameter is not null
                            && parameterTypes.TryGetValue(parameter, out string? type)
                                ? type
                                : null;
                    }

                    fields.Add(
                        new ClientField
                        {
                            Name = IsMeaningfulName(member)
                                ? PacketNaming.SnakeCase(member!)
                                : null,
                            Type = MapActionScriptType(declared),
                            SemanticType = declared,
                            // Anything other than a plain member read is a computed element, and its
                            // wire shape is whatever that computation produces.
                            Note = expression.Contains('(', StringComparison.Ordinal)
                                ? $"computed at send time: {SourceTextScanner.Flatten(expression)}"
                                : null,
                        }
                    );
                }
            }
        }

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
                SourceTextScanner.LineAt(text, array.Index)
            ),
        };
    }

    /// <summary>
    /// Maps a private backing field to the public getter that exposes it. The getters are the
    /// client's own words for these values and are the best field names available for this build.
    /// </summary>
    private static Dictionary<string, string> ReadGetterNames(string masked)
    {
        Dictionary<string, string> names = new(StringComparer.Ordinal);

        foreach (Match getter in Getter().Matches(masked))
        {
            string name = getter.Groups[1].Value;
            string backing = getter.Groups[3].Value;

            if (IsMeaningfulName(name))
            {
                names[backing] = name;
            }
        }

        return names;
    }

    /// <summary>
    /// The declared type of every field on the class. Obfuscation renames these but does not retype
    /// them, so <c>private var _SafeStr_4841:int;</c> still says the first value on the wire is an
    /// int even where nothing says what it means.
    /// </summary>
    private static Dictionary<string, string> ReadFieldTypes(string masked)
    {
        Dictionary<string, string> types = new(StringComparer.Ordinal);

        foreach (Match field in FieldDeclaration().Matches(masked))
        {
            types[field.Groups[1].Value] = field.Groups[2].Value;
        }

        return types;
    }

    private static Dictionary<string, string> ReadConstructorParameterTypes(
        string masked,
        string className
    )
    {
        Dictionary<string, string> types = new(StringComparer.Ordinal);
        int constructor = masked.IndexOf($"function {className}(", StringComparison.Ordinal);

        if (constructor < 0)
        {
            return types;
        }

        int open = masked.IndexOf('(', constructor);
        int close = masked.IndexOf(')', open);

        if (open < 0 || close < 0)
        {
            return types;
        }

        foreach (Match parameter in ParameterDeclaration().Matches(masked[open..close]))
        {
            types[parameter.Groups[1].Value] = parameter.Groups[2].Value;
        }

        return types;
    }

    private static Dictionary<string, string> ReadConstructorAssignments(
        string masked,
        string className
    )
    {
        Dictionary<string, string> assignments = new(StringComparer.Ordinal);
        int constructor = masked.IndexOf($"function {className}(", StringComparison.Ordinal);

        if (constructor < 0)
        {
            return assignments;
        }

        (int Start, int End)? body = SourceTextScanner.BlockAfter(masked, constructor);

        if (body is null)
        {
            return assignments;
        }

        foreach (
            Match assignment in SourceTextScanner.Matches(
                masked,
                ConstructorAssignment(),
                body.Value.Start,
                body.Value.End
            )
        )
        {
            assignments[assignment.Groups[1].Value] = assignment.Groups[2].Value;
        }

        return assignments;
    }

    /// <summary>The field a read lands in, taken from the assignment on the same line.</summary>
    private static string? AssignmentTargetBefore(string masked, int start, int index)
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
            // `_items.push(param1.readInteger())` inside a loop: the collection is the target.
            int push = line.IndexOf(".push(", StringComparison.Ordinal);

            return push > 0 ? LastIdentifier(line[..push]) : null;
        }

        return LastIdentifier(line[..assign]);
    }

    private static string? LastIdentifier(string text)
    {
        int end = text.Length;

        while (end > 0 && !char.IsLetterOrDigit(text[end - 1]) && text[end - 1] != '_')
        {
            end--;
        }

        int begin = end;

        while (begin > 0 && (char.IsLetterOrDigit(text[begin - 1]) || text[begin - 1] == '_'))
        {
            begin--;
        }

        return end > begin ? text[begin..end] : null;
    }

    /// <summary>
    /// Rejects names the obfuscator generated. <c>_SafeStr_8797</c> and <c>_loc3_</c> are not field
    /// names, and recording them as such would put invented vocabulary into the specs.
    /// </summary>
    private static bool IsMeaningfulName(string? name) =>
        name is not null
        && name.Length > 1
        && !name.StartsWith("_SafeStr_", StringComparison.Ordinal)
        && !name.StartsWith("_SafeCls_", StringComparison.Ordinal)
        && !name.StartsWith("_SafePkg_", StringComparison.Ordinal)
        && !name.StartsWith("_loc", StringComparison.Ordinal)
        && !name.StartsWith("param", StringComparison.Ordinal);

    private static IEnumerable<string> SplitTopLevel(string text)
    {
        int depth = 0;
        int start = 0;

        for (int i = 0; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '(' or '[' or '{':
                    depth++;
                    break;
                case ')' or ']' or '}':
                    depth--;
                    break;
                case ',' when depth == 0:
                    yield return text[start..i];
                    start = i + 1;
                    break;
            }
        }

        if (start < text.Length && text[start..].Trim().Length > 0)
        {
            yield return text[start..];
        }
    }

    private static WireType MapActionScriptType(string? declared) =>
        declared switch
        {
            "int" or "uint" => WireType.Int32,
            "String" => WireType.String,
            "Boolean" => WireType.Boolean,
            "Number" => WireType.Double,
            "Array" or "Vector" => WireType.Array,
            _ => WireType.Unknown,
        };

    private EvidenceRef Evidence(EvidenceKind kind, string file, string symbol, int line) =>
        new()
        {
            Kind = kind,
            Authority = EvidenceAuthority.ClientCode,
            Origin = Origin,
            Source = workspace.Relative(file),
            Symbol = symbol,
            Line = line,
            Note = tree.Revision is null ? null : $"official client build {tree.Revision}",
        };
}
