using System.Collections.Generic;

namespace Vortex.Specs.Model;

public enum ConflictKind
{
    Unknown = 0,

    /// <summary>Sources disagree on how many fields a packet carries.</summary>
    FieldCount,

    /// <summary>Sources agree on the count but not on the type at some index.</summary>
    FieldType,

    /// <summary>Sources name the same index differently.</summary>
    FieldName,

    /// <summary>Two registries for the same revision give one name two ids.</summary>
    HeaderId,

    /// <summary>Implementations emit different packets for the same trigger.</summary>
    Behaviour,

    /// <summary>Implementations send the same packet to different recipients.</summary>
    Recipient,

    /// <summary>Implementations emit the same packets in a different order.</summary>
    Ordering,
}

public sealed record ConflictPosition
{
    public required string Origin { get; init; }

    public required EvidenceAuthority Authority { get; init; }

    /// <summary>What this source claims, in its own terms.</summary>
    public required string Claim { get; init; }

    public required EvidenceRef Evidence { get; init; }
}

/// <summary>
/// A disagreement between sources, kept rather than resolved. Nothing in this pipeline is permitted
/// to silently pick a winner: when the official answer is not known, the conflict document is the
/// answer, and it stays open until evidence closes it.
/// </summary>
public sealed record ConflictSpec
{
    public required string Id { get; init; }

    public required ConflictKind Kind { get; init; }

    /// <summary>What is disputed: a packet name, a feature id, or a narrower subject inside one.</summary>
    public required string Subject { get; init; }

    public string? PacketName { get; init; }

    public string? FeatureId { get; init; }

    public required IReadOnlyList<ConflictPosition> Positions { get; init; }

    /// <summary>
    /// Always <see cref="Confidence.Unknown"/> unless an official-grade source settles it. Two fan
    /// implementations agreeing against a third is a majority, not a fact.
    /// </summary>
    public Confidence OfficialStatus { get; init; } = Confidence.Unknown;

    public string? Resolution { get; init; }
}
