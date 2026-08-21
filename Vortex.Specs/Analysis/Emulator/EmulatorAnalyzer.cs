using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Vortex.Specs.Model;
using Vortex.Specs.Naming;
using Vortex.Specs.Sources;

namespace Vortex.Specs.Analysis.Emulator;

/// <summary>
/// Reads this repository's protocol layer: the header table, the revision maps that bind headers to
/// parsers and composer types to serializers, the wire layouts on both sides, and the handler each
/// incoming message reaches.
/// </summary>
public sealed class EmulatorAnalyzer(SpecWorkspace workspace, CSharpSourceIndex index)
{
    private const string Origin = "vortex";

    private readonly WireLayoutExtractor _layouts = new(index);

    public EmulatorScan Scan()
    {
        HeaderTable headers = ReadHeaders();
        RevisionMapping mapping = ReadRevisionMaps();
        string revision = ReadRevisionString() ?? SpecConstants.TargetRevision;

        List<EmulatorIncoming> incoming = [.. BuildIncoming(headers, mapping)];
        List<EmulatorOutgoing> outgoing = [.. BuildOutgoing(headers, mapping)];
        List<EmulatorFlow> flows = [.. new EmulatorFlowAnalyzer(workspace, index).Scan()];

        Dictionary<string, EmulatorFlow> flowsByMessage = flows
            .GroupBy(f => f.MessageType, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        Dictionary<string, string> handlerByMessage = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, EmulatorFlow> flow in flowsByMessage)
        {
            handlerByMessage[flow.Key] = flow.Value.HandlerType;
        }

        incoming =
        [
            .. incoming.Select(i =>
                i.MessageType is not null
                && handlerByMessage.TryGetValue(i.MessageType, out string? handler)
                    ? i with
                    {
                        HandlerType = handler,
                        HandlerEvidence = HandlerEvidence(handler),
                    }
                    : i
            ),
        ];

        return new EmulatorScan
        {
            Revision = revision,
            Incoming = incoming,
            Outgoing = outgoing,
            Flows = flows,
            Registry = BuildRegistry(revision, headers),
            UnmappedHeaderConstants =
            [
                .. headers
                    .Incoming.Keys.Where(k => !mapping.ParsersByHeader.ContainsKey(k))
                    .Concat(
                        headers.Outgoing.Keys.Where(k => !mapping.HeaderConstantsInUse.Contains(k))
                    )
                    .OrderBy(k => k, StringComparer.Ordinal),
            ],
            UnmappedImplementations = FindUnmappedImplementations(mapping),
        };
    }

    // ---------------------------------------------------------------- headers

    private sealed record HeaderTable(
        IReadOnlyDictionary<string, int> Incoming,
        IReadOnlyDictionary<string, int> Outgoing,
        IndexedType? IncomingType,
        IndexedType? OutgoingType
    );

    private HeaderTable ReadHeaders()
    {
        IndexedType? incomingType = index
            .FindByName("MessageEvent")
            .FirstOrDefault(t => t.Declaration is ClassDeclarationSyntax);
        IndexedType? outgoingType = index
            .FindByName("MessageComposer")
            .FirstOrDefault(t => t.Declaration is ClassDeclarationSyntax);

        return new HeaderTable(
            ReadConstants(incomingType),
            ReadConstants(outgoingType),
            incomingType,
            outgoingType
        );
    }

    private static IReadOnlyDictionary<string, int> ReadConstants(IndexedType? type)
    {
        Dictionary<string, int> constants = new(StringComparer.Ordinal);

        if (type is null)
        {
            return constants;
        }

        foreach (
            FieldDeclarationSyntax field in type.Declaration.Members.OfType<FieldDeclarationSyntax>()
        )
        {
            if (!field.Modifiers.Any(m => m.ValueText == "const"))
            {
                continue;
            }

            foreach (VariableDeclaratorSyntax declarator in field.Declaration.Variables)
            {
                if (
                    declarator.Initializer?.Value is LiteralExpressionSyntax literal
                    && literal.Token.Value is int value
                )
                {
                    constants[declarator.Identifier.ValueText] = value;
                }
            }
        }

        return constants;
    }

    private string? ReadRevisionString()
    {
        foreach (IndexedType type in index.Types)
        {
            foreach (
                PropertyDeclarationSyntax property in type.Declaration.Members.OfType<PropertyDeclarationSyntax>()
            )
            {
                if (
                    property.Identifier.ValueText != "Revision"
                    || property.ExpressionBody?.Expression is not LiteralExpressionSyntax literal
                )
                {
                    continue;
                }

                if (literal.Token.Value is string revision && revision.Length > 0)
                {
                    return revision;
                }
            }
        }

        return null;
    }

    // ------------------------------------------------------------------- maps

    private sealed record RevisionMapping(
        IReadOnlyDictionary<string, string> ParsersByHeader,
        IReadOnlyDictionary<
            string,
            (string Serializer, string? HeaderConstant)
        > SerializersByComposer,
        IReadOnlyCollection<string> HeaderConstantsInUse
    );

    /// <summary>
    /// Reads the <c>Maps/*.cs</c> registration calls. This is the binding that actually decides what
    /// the emulator answers on the wire — a parser class with no <c>MapParser</c> call behind it is
    /// unreachable no matter how correct it is.
    /// </summary>
    private RevisionMapping ReadRevisionMaps()
    {
        Dictionary<string, string> parsers = new(StringComparer.Ordinal);
        Dictionary<string, (string, string?)> serializers = new(StringComparer.Ordinal);
        HashSet<string> constantsInUse = new(StringComparer.Ordinal);

        foreach (IndexedType type in index.Types)
        {
            if (!type.BaseTypes.Contains("IRevisionMap"))
            {
                continue;
            }

            foreach (
                InvocationExpressionSyntax invocation in type
                    .Declaration.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
            )
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax member)
                {
                    continue;
                }

                SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;

                switch (member.Name.Identifier.ValueText)
                {
                    case "MapParser" when arguments.Count == 2:
                    {
                        string? header = HeaderConstantName(arguments[0].Expression);
                        string? parser = ConstructedTypeName(arguments[1].Expression);

                        if (header is not null && parser is not null)
                        {
                            parsers[header] = parser;
                            constantsInUse.Add(header);
                        }

                        break;
                    }

                    case "MapSerializer" when arguments.Count == 2:
                    {
                        string? composer = TypeOfArgument(arguments[0].Expression);
                        string? serializer = ConstructedTypeName(arguments[1].Expression);
                        string? header = FirstHeaderConstantIn(arguments[1].Expression);

                        if (composer is not null && serializer is not null)
                        {
                            serializers[composer] = (serializer, header);

                            if (header is not null)
                            {
                                constantsInUse.Add(header);
                            }
                        }

                        break;
                    }
                }
            }
        }

        return new RevisionMapping(parsers, serializers, constantsInUse);
    }

    private static string? HeaderConstantName(ExpressionSyntax expression) =>
        expression is MemberAccessExpressionSyntax member ? member.Name.Identifier.ValueText : null;

    private static string? TypeOfArgument(ExpressionSyntax expression) =>
        expression is TypeOfExpressionSyntax typeOf ? typeOf.Type.ToString() : null;

    private static string? ConstructedTypeName(ExpressionSyntax expression) =>
        expression is ObjectCreationExpressionSyntax creation ? creation.Type.ToString() : null;

    private static string? FirstHeaderConstantIn(ExpressionSyntax expression) =>
        expression
            .DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Where(m => m.Expression.ToString() is "MessageComposer" or "MessageEvent")
            .Select(m => m.Name.Identifier.ValueText)
            .FirstOrDefault();

    // --------------------------------------------------------------- packets

    private IEnumerable<EmulatorIncoming> BuildIncoming(
        HeaderTable headers,
        RevisionMapping mapping
    )
    {
        foreach (
            KeyValuePair<string, string> pair in mapping.ParsersByHeader.OrderBy(
                p => p.Key,
                StringComparer.Ordinal
            )
        )
        {
            IndexedType? parser = index.FindSingle(pair.Value);
            WireLayout layout = parser is null
                ? new WireLayout([], IsPartial: true, ["parser type not found"])
                : _layouts.ExtractRead(parser.Declaration);

            yield return new EmulatorIncoming
            {
                Canonical = PacketNaming.Canonical(pair.Key),
                HeaderConstant = pair.Key,
                HeaderId = headers.Incoming.TryGetValue(pair.Key, out int id) ? id : null,
                ParserType = pair.Value,
                MessageType = parser is null ? null : MessageTypeOf(parser),
                Layout = layout.Ops,
                LayoutIsPartial = layout.IsPartial,
                ParserEvidence = parser is null
                    ? null
                    : Evidence(EvidenceKind.EmulatorParser, parser),
                HeaderEvidence = HeaderEvidence(headers.IncomingType, pair.Key),
            };
        }
    }

    private IEnumerable<EmulatorOutgoing> BuildOutgoing(
        HeaderTable headers,
        RevisionMapping mapping
    )
    {
        foreach (
            KeyValuePair<
                string,
                (string Serializer, string? HeaderConstant)
            > pair in mapping.SerializersByComposer.OrderBy(p => p.Key, StringComparer.Ordinal)
        )
        {
            IndexedType? serializer = index.FindSingle(pair.Value.Serializer);
            WireLayout layout = serializer is null
                ? new WireLayout([], IsPartial: true, ["serializer type not found"])
                : _layouts.ExtractWrite(serializer.Declaration);

            yield return new EmulatorOutgoing
            {
                Canonical = PacketNaming.Canonical(pair.Key),
                ComposerType = pair.Key,
                SerializerType = pair.Value.Serializer,
                HeaderConstant = pair.Value.HeaderConstant,
                HeaderId =
                    pair.Value.HeaderConstant is not null
                    && headers.Outgoing.TryGetValue(pair.Value.HeaderConstant, out int id)
                        ? id
                        : null,
                Layout = layout.Ops,
                LayoutIsPartial = layout.IsPartial,
                SerializerEvidence = serializer is null
                    ? null
                    : Evidence(EvidenceKind.EmulatorSerializer, serializer),
                HeaderEvidence = HeaderEvidence(headers.OutgoingType, pair.Value.HeaderConstant),
            };
        }
    }

    /// <summary>The message record a parser constructs — the type handlers are registered against.</summary>
    private static string? MessageTypeOf(IndexedType parser)
    {
        foreach (
            ObjectCreationExpressionSyntax creation in parser
                .Declaration.DescendantNodes()
                .OfType<ObjectCreationExpressionSyntax>()
        )
        {
            string name = creation.Type.ToString();

            if (name.EndsWith("Message", StringComparison.Ordinal))
            {
                return name;
            }
        }

        return null;
    }

    private IReadOnlyList<string> FindUnmappedImplementations(RevisionMapping mapping)
    {
        HashSet<string> mapped = new(mapping.ParsersByHeader.Values, StringComparer.Ordinal);

        foreach ((string serializer, string? _) in mapping.SerializersByComposer.Values)
        {
            mapped.Add(serializer);
        }

        List<string> unmapped = [];

        foreach (IndexedType type in index.Types)
        {
            string path = workspace.Relative(type.File.Path);

            if (!path.Contains("/Revision", StringComparison.Ordinal))
            {
                continue;
            }

            bool isImplementation =
                type.BaseTypes.Contains("IParser")
                || type.BaseTypes.Any(b =>
                    b.StartsWith("AbstractSerializer<", StringComparison.Ordinal)
                )
                || type.BaseTypes.Contains("ISerializer");

            if (isImplementation && !mapped.Contains(type.Name))
            {
                unmapped.Add($"{type.Name} ({path})");
            }
        }

        unmapped.Sort(StringComparer.Ordinal);
        return unmapped;
    }

    private RevisionRegistry BuildRegistry(string revision, HeaderTable headers) =>
        new()
        {
            Id = revision,
            RevisionString = revision,
            Origin = Origin,
            Authority = EvidenceAuthority.VortexEmulator,
            TargetsSameRevision = true,
            Incoming = HeaderTableFolder.Fold(headers.Incoming).Table,
            Outgoing = HeaderTableFolder.Fold(headers.Outgoing).Table,
            Evidence = new EvidenceRef
            {
                Kind = EvidenceKind.EmulatorHeader,
                Authority = EvidenceAuthority.VortexEmulator,
                Origin = Origin,
                Source = headers.IncomingType is null
                    ? "Vortex.Revisions"
                    : workspace.Relative(headers.IncomingType.File.Path),
                Symbol = "MessageEvent / MessageComposer",
                Note = "the header table this emulator answers on; ids are revision-specific",
            },
        };

    private EvidenceRef Evidence(EvidenceKind kind, IndexedType type) =>
        new()
        {
            Kind = kind,
            Authority = EvidenceAuthority.VortexEmulator,
            Origin = Origin,
            Source = workspace.Relative(type.File.Path),
            Symbol = type.Name,
            Line = type.Line,
        };

    private EvidenceRef? HeaderEvidence(IndexedType? table, string? constant)
    {
        if (table is null || constant is null)
        {
            return null;
        }

        return new EvidenceRef
        {
            Kind = EvidenceKind.EmulatorHeader,
            Authority = EvidenceAuthority.VortexEmulator,
            Origin = Origin,
            Source = workspace.Relative(table.File.Path),
            Symbol = constant,
        };
    }

    private EvidenceRef? HandlerEvidence(string handlerType)
    {
        IndexedType? handler = index.FindSingle(handlerType);

        return handler is null ? null : Evidence(EvidenceKind.EmulatorHandler, handler);
    }
}
