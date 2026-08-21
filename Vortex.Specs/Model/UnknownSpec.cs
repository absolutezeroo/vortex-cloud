using System.Collections.Generic;

namespace Vortex.Specs.Model;

public enum UnknownSeverity
{
    /// <summary>Cosmetic — a field name nobody has attested, on a packet that works.</summary>
    Low = 0,

    /// <summary>Worth answering before building on top of it.</summary>
    Medium = 1,

    /// <summary>Someone will implement a guess here if this is not answered first.</summary>
    Critical = 2,
}

/// <summary>
/// Something the sources do not answer. Written down deliberately: an unknown that is recorded can
/// be closed by the next capture, while an unknown that was quietly filled in with a plausible value
/// never gets revisited because nothing marks it as open.
/// </summary>
public sealed record UnknownSpec
{
    public required string Id { get; init; }

    public required string Subject { get; init; }

    public required string Question { get; init; }

    public required UnknownSeverity Severity { get; init; }

    public string? PacketName { get; init; }

    public string? FeatureId { get; init; }

    /// <summary>What would close it, in terms of an action someone can take.</summary>
    public required string ResolvedBy { get; init; }

    /// <summary>What is known already, so the question starts from the current state.</summary>
    public IReadOnlyList<EvidenceRef> KnownEvidence { get; init; } = [];
}
