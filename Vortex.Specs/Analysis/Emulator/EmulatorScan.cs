using System.Collections.Generic;
using Vortex.Specs.Model;

namespace Vortex.Specs.Analysis.Emulator;

/// <summary>An incoming packet as this repository maps it: header, parser, message type, handler.</summary>
public sealed record EmulatorIncoming
{
    public required string Canonical { get; init; }

    public required string HeaderConstant { get; init; }

    public int? HeaderId { get; init; }

    public string? ParserType { get; init; }

    public string? MessageType { get; init; }

    public string? HandlerType { get; init; }

    public IReadOnlyList<WireOp> Layout { get; init; } = [];

    public bool LayoutIsPartial { get; init; }

    public EvidenceRef? ParserEvidence { get; init; }

    public EvidenceRef? HeaderEvidence { get; init; }

    public EvidenceRef? HandlerEvidence { get; init; }
}

/// <summary>An outgoing packet as this repository maps it: composer type, serializer, header.</summary>
public sealed record EmulatorOutgoing
{
    public required string Canonical { get; init; }

    public required string ComposerType { get; init; }

    public string? SerializerType { get; init; }

    public string? HeaderConstant { get; init; }

    public int? HeaderId { get; init; }

    public IReadOnlyList<WireOp> Layout { get; init; } = [];

    public bool LayoutIsPartial { get; init; }

    public EvidenceRef? SerializerEvidence { get; init; }

    public EvidenceRef? HeaderEvidence { get; init; }
}

/// <summary>
/// The behaviour reachable from one handler, as observed in this repository's own code.
/// </summary>
/// <remarks>
/// Everything here is recorded under <see cref="EvidenceAuthority.VortexEmulator"/>. That this
/// emulator validates a tile before moving furniture is a fact about this emulator; whether Habbo
/// does the same is a separate question the capture importer answers, not this one.
/// </remarks>
public sealed record EmulatorFlow
{
    public required string HandlerType { get; init; }

    public required string MessageType { get; init; }

    /// <summary>The first domain operation the handler delegates to. Anchors the feature id.</summary>
    public string? PrimaryOperation { get; init; }

    public IReadOnlyList<FeatureFlowStep> Steps { get; init; } = [];

    public IReadOnlyList<FeatureCheck> Checks { get; init; } = [];

    public IReadOnlyList<FeatureMutation> Mutations { get; init; } = [];

    public IReadOnlyList<FeatureOutgoing> Outgoing { get; init; } = [];

    public bool ReachesPersistence { get; init; }

    /// <summary>True when the handler body only forwards, with no logic of its own.</summary>
    public bool IsOrchestrationOnly { get; init; }

    /// <summary>Calls the walker could not follow, so a reader knows the flow is not exhaustive.</summary>
    public IReadOnlyList<string> Unresolved { get; init; } = [];
}

public sealed record EmulatorScan
{
    public required string Revision { get; init; }

    public required IReadOnlyList<EmulatorIncoming> Incoming { get; init; }

    public required IReadOnlyList<EmulatorOutgoing> Outgoing { get; init; }

    public required IReadOnlyList<EmulatorFlow> Flows { get; init; }

    public required RevisionRegistry Registry { get; init; }

    /// <summary>
    /// Header constants declared but never mapped to a parser or serializer. A constant nothing
    /// reaches is dead weight at best and a silently unreachable feature at worst, so it is
    /// reported rather than dropped.
    /// </summary>
    public required IReadOnlyList<string> UnmappedHeaderConstants { get; init; }

    /// <summary>Parser and serializer classes that exist but are in no revision map.</summary>
    public required IReadOnlyList<string> UnmappedImplementations { get; init; }
}
