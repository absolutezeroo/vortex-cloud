using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Vortex.Specs.Analysis.Client;
using Vortex.Specs.Analysis.Emulator;
using Vortex.Specs.Analysis.Reference;
using Vortex.Specs.Model;
using Vortex.Specs.Naming;

namespace Vortex.Specs.Reasoning;

/// <summary>
/// Reconciles every source's view of a packet into one spec, keeping each source's own view intact
/// alongside it.
/// </summary>
/// <remarks>
/// Two things this deliberately does not do. It does not average layouts: where sources disagree the
/// strongest one supplies the shape and the disagreement becomes a conflict document, because a
/// merged layout no source ever claimed is worse than either. And it does not invent field names: an
/// index nothing names comes out as <c>unknown_&lt;index&gt;</c>, which a later scan can fill in and
/// which nobody will mistake for knowledge.
/// </remarks>
public sealed class PacketResolver
{
    private sealed record Identity(PacketDirection Direction, string Name);

    private sealed record SourceLayout(
        string Origin,
        EvidenceAuthority Authority,
        IReadOnlyList<PacketFieldSpec> Fields,
        EvidenceRef Evidence,
        bool IsPartial
    );

    public IReadOnlyList<PacketSpec> Resolve(SpecWorld world, IList<string>? notes = null)
    {
        Dictionary<Identity, List<SourceLayout>> layouts = [];
        Dictionary<Identity, string> handlers = new();
        HashSet<Identity> mappedInVortex = [];
        Dictionary<string, string> domains = new(StringComparer.Ordinal);

        AddEmulator(world.Emulator, layouts, handlers, mappedInVortex, domains);

        foreach (ClientScan client in world.Clients)
        {
            int skipped = AddClient(client, world.Emulator.Registry, layouts, domains);

            // Never dropped quietly. These are real messages in a real client that this workspace
            // has no vocabulary for — obfuscated classes from a build whose header ids do not line
            // up with ours — and the count is the size of the blind spot.
            if (skipped > 0 && notes is not null)
            {
                notes.Add(
                    $"{client.Origin}: {skipped} classes carry no usable name and no header id that "
                        + "joins to this build, so they are absent from the specs"
                );
            }
        }

        foreach (ReferenceScan reference in world.References)
        {
            AddReference(reference, layouts, domains);
        }

        List<PacketSpec> specs = [];

        foreach (
            KeyValuePair<Identity, List<SourceLayout>> entry in layouts
                .OrderBy(e => e.Key.Direction)
                .ThenBy(e => e.Key.Name, StringComparer.Ordinal)
        )
        {
            specs.Add(BuildSpec(entry.Key, entry.Value, handlers, mappedInVortex, domains));
        }

        return specs;
    }

    private static PacketSpec BuildSpec(
        Identity identity,
        List<SourceLayout> sources,
        IReadOnlyDictionary<Identity, string> handlers,
        IReadOnlySet<Identity> mappedInVortex,
        IReadOnlyDictionary<string, string> domains
    )
    {
        // Strongest first, then by origin so equal authorities never swap between runs.
        List<SourceLayout> ranked =
        [
            .. sources.OrderBy(s => (int)s.Authority).ThenBy(s => s.Origin, StringComparer.Ordinal),
        ];

        SourceLayout? shape = ranked.FirstOrDefault(s => !s.IsPartial) ?? ranked.FirstOrDefault();

        IReadOnlyList<PacketFieldSpec> fields = shape is null ? [] : MergeFieldNames(shape, ranked);

        List<EvidenceAuthority> agreeing =
        [
            .. ranked.Where(s => !s.IsPartial && SameShape(shape, s)).Select(s => s.Authority),
        ];

        return new PacketSpec
        {
            Name = identity.Name,
            Direction = identity.Direction,
            Domain = domains.TryGetValue(identity.Name, out string? domain) ? domain : "unsorted",
            Fields = fields,
            StructureConfidence = ConfidencePolicy.Combine(agreeing),
            Evidence =
            [
                .. ranked.Select(s => s.Evidence).Order(EvidenceRef.ByAuthorityThenId.Instance),
            ],
            Observations =
            [
                .. ranked.Select(s => new PacketLayoutObservation
                {
                    Origin = s.Origin,
                    Authority = s.Authority,
                    Fields = s.Fields,
                    Evidence = s.Evidence,
                    IsPartial = s.IsPartial,
                }),
            ],
            MappedInVortex = mappedInVortex.Contains(identity),
            VortexHandler = handlers.TryGetValue(identity, out string? handler) ? handler : null,
        };
    }

    /// <summary>
    /// Takes the shape from the strongest complete source and fills each field's name from the
    /// strongest source that has one at that index.
    /// </summary>
    /// <remarks>
    /// Names and shapes come from different places on purpose. The official client fixes the shape
    /// for this build but has its identifiers obfuscated away; Nitro names everything but is a
    /// reimplementation. Taking shape from the first and names from the second — each recorded with
    /// its own confidence — is strictly better than picking one source and living with its blind spot.
    /// </remarks>
    private static IReadOnlyList<PacketFieldSpec> MergeFieldNames(
        SourceLayout shape,
        IReadOnlyList<SourceLayout> ranked
    )
    {
        // Compared as flat sequences of leaves, because sources group the same bytes differently:
        // one expands a shared sub-serializer inline, another keeps it as a nested block. The byte
        // order is the thing they agree on, so that is what the comparison uses.
        List<PacketFieldSpec> shapeLeaves = [.. Leaves(shape.Fields)];
        Borrowed[] borrowed =
        [
            .. Enumerable.Range(0, shapeLeaves.Count).Select(_ => new Borrowed()),
        ];

        foreach (SourceLayout candidate in ranked)
        {
            if (ReferenceEquals(candidate, shape))
            {
                continue;
            }

            List<PacketFieldSpec> candidateLeaves = [.. Leaves(candidate.Fields)];

            if (candidateLeaves.Count != shapeLeaves.Count)
            {
                // A source describing a different number of values is describing a different shape;
                // its name at position i is not a name for this field.
                continue;
            }

            Confidence confidence = ConfidencePolicy.FromAuthority(candidate.Authority);

            for (int i = 0; i < shapeLeaves.Count; i++)
            {
                PacketFieldSpec other = candidateLeaves[i];

                // The shape source can be complete about the layout and still not know what a value
                // means — an obfuscated client gives order and arity but only sometimes a type. A
                // gap it leaves is filled by the next source down, at that source's confidence,
                // rather than being reported as unknown when something does know.
                if (
                    borrowed[i].Type is null
                    && shapeLeaves[i].Type == WireType.Unknown
                    && other.Type != WireType.Unknown
                )
                {
                    borrowed[i].Type = other.Type;
                    borrowed[i].TypeConfidence = confidence;
                    borrowed[i].TypeEvidenceId = candidate.Evidence.Id;
                }

                if (borrowed[i].Name is not null || other.IsPlaceholderName)
                {
                    continue;
                }

                WireType effectiveType = borrowed[i].Type ?? shapeLeaves[i].Type;

                if (other.Type != effectiveType)
                {
                    continue;
                }

                borrowed[i].Name = other.Name;
                borrowed[i].NameConfidence = confidence;
                borrowed[i].NameEvidenceId = candidate.Evidence.Id;
            }
        }

        int cursor = 0;

        return Rebuild(shape.Fields, shape, borrowed, ref cursor);
    }

    /// <summary>What a weaker source could supply for a leaf the strongest source left open.</summary>
    private sealed class Borrowed
    {
        public string? Name { get; set; }

        public Confidence NameConfidence { get; set; } = Confidence.Unknown;

        public string? NameEvidenceId { get; set; }

        public WireType? Type { get; set; }

        public Confidence TypeConfidence { get; set; } = Confidence.Unknown;

        public string? TypeEvidenceId { get; set; }
    }

    private static IEnumerable<PacketFieldSpec> Leaves(IReadOnlyList<PacketFieldSpec> fields)
    {
        foreach (PacketFieldSpec field in fields)
        {
            if (field.Children.Count == 0)
            {
                yield return field;
                continue;
            }

            foreach (PacketFieldSpec leaf in Leaves(field.Children))
            {
                yield return leaf;
            }
        }
    }

    private static IReadOnlyList<PacketFieldSpec> Rebuild(
        IReadOnlyList<PacketFieldSpec> fields,
        SourceLayout shape,
        IReadOnlyList<Borrowed> borrowed,
        ref int cursor
    )
    {
        Confidence shapeConfidence = ConfidencePolicy.FromAuthority(shape.Authority);
        List<PacketFieldSpec> rebuilt = [];

        for (int i = 0; i < fields.Count; i++)
        {
            PacketFieldSpec field = fields[i];

            if (field.Children.Count > 0)
            {
                rebuilt.Add(
                    field with
                    {
                        Index = i,
                        NameConfidence = field.IsPlaceholderName
                            ? Confidence.Unknown
                            : shapeConfidence,
                        TypeConfidence = shapeConfidence,
                        EvidenceIds = [shape.Evidence.Id],
                        Children = Rebuild(field.Children, shape, borrowed, ref cursor),
                    }
                );

                continue;
            }

            Borrowed loan = cursor < borrowed.Count ? borrowed[cursor] : new Borrowed();
            cursor++;

            bool keepOwnName = !field.IsPlaceholderName;
            bool keepOwnType = field.Type != WireType.Unknown || loan.Type is null;

            List<string> evidenceIds = [shape.Evidence.Id];

            if (!keepOwnName && loan.NameEvidenceId is not null)
            {
                evidenceIds.Add(loan.NameEvidenceId);
            }

            if (!keepOwnType && loan.TypeEvidenceId is not null)
            {
                evidenceIds.Add(loan.TypeEvidenceId);
            }

            rebuilt.Add(
                field with
                {
                    Index = i,
                    Name = keepOwnName ? field.Name : loan.Name ?? field.Name,
                    Type = keepOwnType ? field.Type : loan.Type!.Value,
                    NameConfidence = keepOwnName ? shapeConfidence : loan.NameConfidence,
                    TypeConfidence = keepOwnType ? shapeConfidence : loan.TypeConfidence,
                    EvidenceIds = [.. evidenceIds.Distinct(StringComparer.Ordinal)],
                }
            );
        }

        return rebuilt;
    }

    /// <summary>
    /// Keeps the declared type only when it adds something the wire type does not already say.
    /// <c>RoomObjectId</c> is worth recording next to an int32; <c>int</c> is not.
    /// </summary>
    private static string? MeaningfulSemanticType(string? declared) =>
        declared is null
        || declared
            is "int"
                or "uint"
                or "Int32"
                or "long"
                or "short"
                or "byte"
                or "bool"
                or "Boolean"
                or "string"
                or "String"
                or "Number"
                or "number"
                or "double"
                or "float"
            ? null
            : declared;

    public static string PlaceholderName(int index) =>
        "unknown_" + index.ToString(CultureInfo.InvariantCulture);

    private static bool SameShape(SourceLayout? a, SourceLayout b)
    {
        if (a is null)
        {
            return false;
        }

        if (ReferenceEquals(a, b))
        {
            return true;
        }

        List<PacketFieldSpec> left = [.. Leaves(a.Fields)];
        List<PacketFieldSpec> right = [.. Leaves(b.Fields)];

        if (left.Count != right.Count)
        {
            return false;
        }

        for (int i = 0; i < left.Count; i++)
        {
            // An unknown type on either side is a gap, not a contradiction: the source established
            // the arity and order and simply did not say what the value is.
            if (
                left[i].Type != WireType.Unknown
                && right[i].Type != WireType.Unknown
                && left[i].Type != right[i].Type
            )
            {
                return false;
            }
        }

        return true;
    }

    // -------------------------------------------------------------- emulator

    private static void AddEmulator(
        EmulatorScan scan,
        Dictionary<Identity, List<SourceLayout>> layouts,
        Dictionary<Identity, string> handlers,
        HashSet<Identity> mapped,
        Dictionary<string, string> domains
    )
    {
        foreach (EmulatorIncoming incoming in scan.Incoming)
        {
            Identity identity = new(PacketDirection.Incoming, incoming.Canonical);
            mapped.Add(identity);
            domains.TryAdd(incoming.Canonical, DomainFromPath(incoming.ParserEvidence?.Source));

            if (incoming.HandlerType is not null)
            {
                handlers[identity] = incoming.HandlerType;
            }

            if (incoming.ParserEvidence is not null)
            {
                Add(
                    layouts,
                    identity,
                    new SourceLayout(
                        "vortex",
                        EvidenceAuthority.VortexEmulator,
                        FromWireOps(incoming.Layout),
                        incoming.ParserEvidence,
                        incoming.LayoutIsPartial
                    )
                );
            }
        }

        foreach (EmulatorOutgoing outgoing in scan.Outgoing)
        {
            Identity identity = new(PacketDirection.Outgoing, outgoing.Canonical);
            mapped.Add(identity);
            domains.TryAdd(outgoing.Canonical, DomainFromPath(outgoing.SerializerEvidence?.Source));

            if (outgoing.SerializerEvidence is not null)
            {
                Add(
                    layouts,
                    identity,
                    new SourceLayout(
                        "vortex",
                        EvidenceAuthority.VortexEmulator,
                        FromWireOps(outgoing.Layout),
                        outgoing.SerializerEvidence,
                        outgoing.LayoutIsPartial
                    )
                );
            }
        }
    }

    /// <summary>
    /// Uses the source tree's own folder as the packet's domain. The revision maps and handler
    /// folders already group messages the way this project thinks about them; a second taxonomy
    /// invented here would only disagree with the first.
    /// </summary>
    private static string DomainFromPath(string? path) => PacketNaming.DomainFromSourcePath(path);

    private static IReadOnlyList<PacketFieldSpec> FromWireOps(IReadOnlyList<WireOp> ops)
    {
        List<PacketFieldSpec> fields = [];

        for (int i = 0; i < ops.Count; i++)
        {
            WireOp op = ops[i];

            fields.Add(
                new PacketFieldSpec
                {
                    Index = i,
                    Name = op.Name is null ? PlaceholderName(i) : PacketNaming.SnakeCase(op.Name),
                    Type = op.Type,
                    SemanticType = MeaningfulSemanticType(op.SemanticType),
                    Note = BuildNote(op),
                    Children = FromWireOps(op.Children),
                }
            );
        }

        return fields;
    }

    private static string? BuildNote(WireOp op)
    {
        List<string> parts = [];

        if (op.ConstantValue is not null)
        {
            parts.Add($"written as the constant {op.ConstantValue}");
        }

        if (op.Condition is not null)
        {
            parts.Add($"only when {op.Condition}");
        }

        if (op.Comment is not null)
        {
            parts.Add($"source comment: {op.Comment}");
        }

        return parts.Count == 0 ? null : string.Join("; ", parts);
    }

    // ---------------------------------------------------------------- client

    /// <returns>How many packets had to be skipped for want of a usable identity.</returns>
    private static int AddClient(
        ClientScan client,
        RevisionRegistry vortexRegistry,
        Dictionary<Identity, List<SourceLayout>> layouts,
        Dictionary<string, string> domains
    )
    {
        int skipped = 0;
        Dictionary<int, string> incomingById = ReverseTable(vortexRegistry.Incoming);
        Dictionary<int, string> outgoingById = ReverseTable(vortexRegistry.Outgoing);

        foreach (ClientPacket packet in client.Packets)
        {
            string? name = NameFor(packet, client, incomingById, outgoingById);

            if (name is null)
            {
                skipped++;
                continue;
            }

            Identity identity = new(packet.Direction, name);
            string domain = PacketNaming.DomainFromSourcePath(packet.Evidence.Source);

            if (domain != "unsorted")
            {
                domains.TryAdd(name, domain);
            }

            Add(
                layouts,
                identity,
                new SourceLayout(
                    client.Origin,
                    client.Authority,
                    FromClientFields(packet.Fields),
                    packet.Evidence,
                    packet.IsPartial
                )
            );
        }

        return skipped;
    }

    /// <summary>
    /// Works out which symbolic packet a client class is.
    /// </summary>
    /// <remarks>
    /// For the official client this is the whole trick. Its class names are obfuscated to
    /// <c>_SafeCls_3619</c>, but its registry binds every class to a header id, and those ids are for
    /// the exact build this emulator targets — so the id joins an anonymous class to a name. Ids from
    /// a client targeting a different build are useless for this and are not consulted.
    /// </remarks>
    private static string? NameFor(
        ClientPacket packet,
        ClientScan client,
        IReadOnlyDictionary<int, string> incomingById,
        IReadOnlyDictionary<int, string> outgoingById
    )
    {
        bool synthetic = PacketNaming.IsSyntheticTypeName(packet.DeclaredType);

        if (client.TargetsSameRevision && packet.HeaderId is int header)
        {
            IReadOnlyDictionary<int, string> table =
                packet.Direction == PacketDirection.Incoming ? incomingById : outgoingById;

            if (table.TryGetValue(header, out string? joined))
            {
                return joined;
            }
        }

        // No id join and no usable name: a real class that this workspace has no vocabulary for. It
        // is counted as an unmapped client message by the unknown collector, not named here.
        return synthetic ? null : packet.Canonical;
    }

    private static Dictionary<int, string> ReverseTable(IReadOnlyDictionary<string, int> table)
    {
        Dictionary<int, string> reversed = [];

        foreach (
            KeyValuePair<string, int> entry in table.OrderBy(e => e.Key, StringComparer.Ordinal)
        )
        {
            reversed.TryAdd(entry.Value, entry.Key);
        }

        return reversed;
    }

    private static IReadOnlyList<PacketFieldSpec> FromClientFields(
        IReadOnlyList<ClientField> fields
    )
    {
        List<PacketFieldSpec> specs = [];

        for (int i = 0; i < fields.Count; i++)
        {
            ClientField field = fields[i];

            specs.Add(
                new PacketFieldSpec
                {
                    Index = i,
                    Name = field.Name ?? PlaceholderName(i),
                    Type = field.Type,
                    SemanticType = MeaningfulSemanticType(field.SemanticType),
                    Note = field.Note,
                    Children = FromClientFields(field.Children),
                }
            );
        }

        return specs;
    }

    // ------------------------------------------------------------- reference

    private static void AddReference(
        ReferenceScan reference,
        Dictionary<Identity, List<SourceLayout>> layouts,
        Dictionary<string, string> domains
    )
    {
        foreach (ReferenceBehaviour behaviour in reference.Behaviours)
        {
            string domain = PacketNaming.DomainFromSourcePath(behaviour.Evidence.Source);

            if (domain != "unsorted")
            {
                domains.TryAdd(behaviour.Canonical, domain);
            }

            Add(
                layouts,
                new Identity(PacketDirection.Incoming, behaviour.Canonical),
                new SourceLayout(
                    reference.Origin,
                    reference.Authority,
                    FromClientFields(behaviour.Fields),
                    behaviour.Evidence,
                    behaviour.Fields.Count == 0
                )
            );
        }

        foreach (ReferenceComposerLayout composer in reference.Composers)
        {
            string domain = PacketNaming.DomainFromSourcePath(composer.Evidence.Source);

            if (domain != "unsorted")
            {
                domains.TryAdd(composer.Canonical, domain);
            }

            Add(
                layouts,
                new Identity(PacketDirection.Outgoing, composer.Canonical),
                new SourceLayout(
                    reference.Origin,
                    reference.Authority,
                    FromClientFields(composer.Fields),
                    composer.Evidence,
                    composer.IsPartial
                )
            );
        }
    }

    private static void Add(
        Dictionary<Identity, List<SourceLayout>> layouts,
        Identity identity,
        SourceLayout layout
    )
    {
        if (!layouts.TryGetValue(identity, out List<SourceLayout>? bucket))
        {
            bucket = [];
            layouts[identity] = bucket;
        }

        bucket.Add(layout);
    }
}
