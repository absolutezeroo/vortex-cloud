using System.Collections.Generic;
using Vortex.Specs.Analysis.Client;
using Vortex.Specs.Model;

namespace Vortex.Specs.Analysis.Reference;

/// <summary>What one reference emulator does when a packet arrives.</summary>
public sealed record ReferenceBehaviour
{
    public required string Canonical { get; init; }

    public required string HandlerType { get; init; }

    public IReadOnlyList<ClientField> Fields { get; init; } = [];

    public IReadOnlyList<FeatureCheck> Checks { get; init; } = [];

    /// <summary>Composers sent, in the order the handler sends them.</summary>
    public IReadOnlyList<FeatureOutgoing> Outgoing { get; init; } = [];

    public required EvidenceRef Evidence { get; init; }
}

/// <summary>The layout one reference emulator writes for an outgoing packet.</summary>
public sealed record ReferenceComposerLayout
{
    public required string Canonical { get; init; }

    public required string ComposerType { get; init; }

    public IReadOnlyList<ClientField> Fields { get; init; } = [];

    public bool IsPartial { get; init; }

    public required EvidenceRef Evidence { get; init; }
}

public sealed record ReferenceScan
{
    public required string Origin { get; init; }

    /// <summary>
    /// Always <see cref="EvidenceAuthority.ReferenceEmulator"/> for a single tree. Two independent
    /// reference trees agreeing is what the resolver may promote to
    /// <see cref="EvidenceAuthority.MultiImplementation"/> — never one on its own, however mature.
    /// </summary>
    public required EvidenceAuthority Authority { get; init; }

    public required IReadOnlyList<ReferenceBehaviour> Behaviours { get; init; }

    public required IReadOnlyList<ReferenceComposerLayout> Composers { get; init; }

    public IReadOnlyDictionary<string, int> IncomingHeaders { get; init; } =
        new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> OutgoingHeaders { get; init; } =
        new Dictionary<string, int>();

    public IReadOnlyList<string> Unresolved { get; init; } = [];
}

public interface IReferenceAnalyzer
{
    string Origin { get; }

    ReferenceScan Scan();
}
