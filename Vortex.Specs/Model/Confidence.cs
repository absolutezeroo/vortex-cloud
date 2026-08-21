namespace Vortex.Specs.Model;

/// <summary>
/// The confidence attached to a claim in a spec. Deliberately distinct from
/// <see cref="EvidenceAuthority"/>: authority describes one source, confidence describes the
/// conclusion drawn from every source together.
/// </summary>
public enum Confidence
{
    /// <summary>Nothing supports a value one way or the other. The honest default.</summary>
    Unknown = 0,

    /// <summary>Sources disagree and no rule in this repository is allowed to pick a winner.</summary>
    Conflicting = 1,

    /// <summary>A guess with no evidence behind it.</summary>
    Assumed = 2,

    /// <summary>Derived from evidence rather than observed.</summary>
    Inferred = 3,

    /// <summary>Vortex does this. Says nothing about what Habbo does.</summary>
    ImplementationObserved = 4,

    /// <summary>One third-party reference emulator does this.</summary>
    ReferenceObserved = 5,

    /// <summary>Two or more independent non-Vortex implementations agree.</summary>
    MultiReferenceConfirmed = 6,

    /// <summary>The official client's own code fixes this.</summary>
    ClientConfirmed = 7,

    /// <summary>A capture of an official server shows this.</summary>
    CaptureConfirmed = 8,

    /// <summary>Multiple independent official-grade sources agree. The ceiling.</summary>
    Confirmed = 9,
}
