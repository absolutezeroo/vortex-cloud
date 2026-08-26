using System;
using System.Collections.Generic;
using System.Linq;

namespace Vortex.Specs.Completeness;

/// <summary>
/// What this emulator does about one thing the target client is able to ask for.
/// </summary>
/// <remarks>
/// The ladder only goes up on evidence, and the two rungs people most want to conflate are kept
/// apart: <see cref="Implemented"/> says Vortex has a code path, <see cref="Complete"/> says someone
/// checked it against the protocol and wrote down where. Static analysis can reach the first and
/// must never reach the second.
/// </remarks>
public enum ObligationStatus
{
    /// <summary>The client can send it and Vortex has no entry path for it.</summary>
    Missing,

    /// <summary>An entry path exists; the behaviour behind it is not proven to go anywhere.</summary>
    Partial,

    /// <summary>A meaningful Vortex path exists. Says nothing about official behaviour.</summary>
    Implemented,

    /// <summary>Implemented plus a verification record that cites its evidence.</summary>
    Complete,

    /// <summary>The sources contradict each other badly enough that no answer is safe.</summary>
    Unknown,

    /// <summary>Excluded by an explicit decision carrying a reason, evidence and a decision id.</summary>
    NotApplicable,

    /// <summary>
    /// A candidate in the target client that cannot be bound well enough to score. Held outside the
    /// denominator and printed anyway, because a surface nobody can classify is not a surface nobody
    /// has to implement.
    /// </summary>
    UnresolvedSurface,
}

public static class ObligationStatusNames
{
    private static readonly Dictionary<ObligationStatus, string> Names = new()
    {
        [ObligationStatus.Missing] = "missing",
        [ObligationStatus.Partial] = "partial",
        [ObligationStatus.Implemented] = "implemented",
        [ObligationStatus.Complete] = "complete",
        [ObligationStatus.Unknown] = "unknown",
        [ObligationStatus.NotApplicable] = "not_applicable",
        [ObligationStatus.UnresolvedSurface] = "unresolved_surface",
    };

    /// <summary>Worst first, so every report reads the same way round.</summary>
    public static readonly ObligationStatus[] Scored =
    [
        ObligationStatus.Missing,
        ObligationStatus.Partial,
        ObligationStatus.Implemented,
        ObligationStatus.Complete,
        ObligationStatus.Unknown,
        ObligationStatus.NotApplicable,
    ];

    public static string Wire(this ObligationStatus value) => Names[value];

    public static bool TryParse(string text, out ObligationStatus value)
    {
        foreach (KeyValuePair<ObligationStatus, string> pair in Names)
        {
            if (string.Equals(pair.Value, text, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Key;
                return true;
            }
        }

        value = ObligationStatus.Unknown;
        return false;
    }
}

/// <summary>One thing the target client can send, and what this repository does about it.</summary>
public sealed record Obligation
{
    /// <summary>Stable key used by <c>decisions.yaml</c> and <c>verification.yaml</c>.</summary>
    public required string Id { get; init; }

    /// <summary>
    /// The symbolic packet name, or <c>header:&lt;id&gt;</c> when the client class is obfuscated and
    /// no Vortex header id joins it. The second form is not a failure of the reader: it is a message
    /// this build can send that this emulator has no vocabulary for at all.
    /// </summary>
    public required string Name { get; init; }

    public required string Domain { get; init; }

    public int? HeaderId { get; init; }

    /// <summary>The client class that reads or writes it, obfuscated name included.</summary>
    public required string ClientClass { get; init; }

    public required ObligationStatus Status { get; init; }

    /// <summary>Why the classifier landed here, in the classifier's own words.</summary>
    public required string Reason { get; init; }

    public bool MappedInVortex { get; init; }

    public string? VortexHandler { get; init; }

    public string? FeatureId { get; init; }

    public IReadOnlyList<string> ConflictIds { get; init; } = [];

    public IReadOnlyList<string> UnknownIds { get; init; } = [];

    /// <summary>Set when a <c>decisions.yaml</c> entry excluded this obligation.</summary>
    public string? DecisionId { get; init; }

    /// <summary>Set when a <c>verification.yaml</c> entry promoted it.</summary>
    public string? VerifiedAtCommit { get; init; }
}

/// <summary>The obligations of one domain, so a slice can be chosen inside one area of the code.</summary>
public sealed record DomainSummary
{
    public required string Domain { get; init; }

    public required IReadOnlyList<Obligation> Obligations { get; init; }

    public int Count(ObligationStatus status) => Obligations.Count(o => o.Status == status);
}

public sealed record CompletenessReport
{
    /// <summary>The build the denominator came from, or null when no such client was found.</summary>
    public required string? TargetRevision { get; init; }

    public required string? TargetOrigin { get; init; }

    /// <summary>Obligations inside the denominator, ordered by domain then name.</summary>
    public required IReadOnlyList<Obligation> Obligations { get; init; }

    /// <summary>Candidates held outside the denominator until they can be bound.</summary>
    public required IReadOnlyList<Obligation> UnresolvedSurface { get; init; }

    /// <summary>Ledger and source problems. Any entry here fails the command.</summary>
    public required IReadOnlyList<string> Problems { get; init; }

    /// <summary>True when a same-revision official client supplied the denominator.</summary>
    public bool HasTargetClient => TargetRevision is not null;

    public int Count(ObligationStatus status) => Obligations.Count(o => o.Status == status);

    /// <summary>Obligations Vortex can at least receive: a mapped packet reaching a handler.</summary>
    public int Mapped => Obligations.Count(o => o.MappedInVortex && o.VortexHandler is not null);

    public int Implemented =>
        Count(ObligationStatus.Implemented) + Count(ObligationStatus.Complete);

    public IReadOnlyList<DomainSummary> Domains =>
        [
            .. Obligations
                .GroupBy(o => o.Domain, StringComparer.Ordinal)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new DomainSummary { Domain = g.Key, Obligations = [.. g] }),
        ];

    /// <summary>
    /// A share of the denominator, or <c>n/a</c> when there is no denominator.
    /// </summary>
    /// <remarks>
    /// The one number this program exists to not fake. With no same-revision official client there is
    /// nothing to be a share of, and printing 100% of an empty set is exactly the false answer the
    /// contract forbids.
    /// </remarks>
    public string Share(int part) =>
        !HasTargetClient || Obligations.Count == 0
            ? "n/a"
            // Invariant, not the machine's locale: a generated file that says 88,9% here and 88.9%
            // on the next developer's machine is a diff nobody caused.
            : string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0} / {1} ({2:0.0}%)",
                part,
                Obligations.Count,
                100.0 * part / Obligations.Count
            );
}
