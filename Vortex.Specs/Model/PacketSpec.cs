using System.Collections.Generic;

namespace Vortex.Specs.Model;

/// <summary>
/// Which way a message travels, named from the server's point of view because that is what this
/// repository is. The official client's own trees use the opposite convention, and the client
/// analyzers flip it on the way in rather than leaking two vocabularies into the specs.
/// </summary>
public enum PacketDirection
{
    /// <summary>Client to server.</summary>
    Incoming,

    /// <summary>Server to client.</summary>
    Outgoing,
}

public sealed record PacketFieldSpec
{
    public required int Index { get; init; }

    /// <summary>
    /// The field's name, or <c>unknown_&lt;index&gt;</c>. An honest <c>unknown_3</c> is the correct
    /// output when nothing names the field; it is a placeholder a later scan can fill in, where an
    /// invented name is a lie a later scan has no reason to revisit.
    /// </summary>
    public required string Name { get; init; }

    public required WireType Type { get; init; }

    /// <summary>Confidence in the <em>name</em>. The type is usually far better attested.</summary>
    public Confidence NameConfidence { get; init; } = Confidence.Unknown;

    /// <summary>Confidence in the <em>type and position</em>.</summary>
    public Confidence TypeConfidence { get; init; } = Confidence.Unknown;

    /// <summary>Set when an implementation models this as an enum/struct worth recording.</summary>
    public string? SemanticType { get; init; }

    public string? Note { get; init; }

    public IReadOnlyList<PacketFieldSpec> Children { get; init; } = [];

    public IReadOnlyList<string> EvidenceIds { get; init; } = [];

    public bool IsPlaceholderName => Name.StartsWith("unknown_", System.StringComparison.Ordinal);
}

/// <summary>
/// One source's opinion of a packet's layout, kept whole so the merger can compare shapes instead of
/// averaging them into something no source ever said.
/// </summary>
public sealed record PacketLayoutObservation
{
    public required string Origin { get; init; }

    public required EvidenceAuthority Authority { get; init; }

    public required IReadOnlyList<PacketFieldSpec> Fields { get; init; }

    public required EvidenceRef Evidence { get; init; }

    /// <summary>
    /// True when the source stops short of a full layout — a serializer that delegates to a helper
    /// this analyzer could not follow, for instance. Such a layout must never be used to claim
    /// another source has "extra" fields.
    /// </summary>
    public bool IsPartial { get; init; }
}

public sealed record PacketSpec
{
    /// <summary>Symbolic name, never a numeric id: <c>MoveObject</c>, not <c>1482</c>.</summary>
    public required string Name { get; init; }

    public required PacketDirection Direction { get; init; }

    /// <summary>Coarse grouping used for the on-disk folder, e.g. <c>room</c>.</summary>
    public required string Domain { get; init; }

    public IReadOnlyList<PacketFieldSpec> Fields { get; init; } = [];

    public Confidence StructureConfidence { get; init; } = Confidence.Unknown;

    public IReadOnlyList<EvidenceRef> Evidence { get; init; } = [];

    public IReadOnlyList<PacketLayoutObservation> Observations { get; init; } = [];

    /// <summary>Ids of conflict documents that mention this packet.</summary>
    public IReadOnlyList<string> ConflictIds { get; init; } = [];

    /// <summary>Ids of unknown documents that mention this packet.</summary>
    public IReadOnlyList<string> UnknownIds { get; init; } = [];

    /// <summary>
    /// True when this repository maps the packet. A packet the official client sends that Vortex
    /// does not map is exactly the gap this system exists to surface, so it is recorded rather than
    /// skipped.
    /// </summary>
    public bool MappedInVortex { get; init; }

    /// <summary>The Vortex handler type name, when the packet reaches one.</summary>
    public string? VortexHandler { get; init; }

    public string SpecId =>
        $"{(Direction == PacketDirection.Incoming ? "incoming" : "outgoing")}/{Name}";
}
