namespace Vortex.Specs.Model;

/// <summary>
/// How much weight a single piece of evidence carries. The order is the repository's source-trust
/// ladder: a lower numeric value outranks a higher one. Nothing in the pipeline is allowed to
/// promote a claim above the best authority that actually backs it.
/// </summary>
public enum EvidenceAuthority
{
    /// <summary>A packet trace recorded against an official Habbo server.</summary>
    OfficialCapture = 1,

    /// <summary>
    /// Behaviour the official client cannot work without — it hard-depends on the server doing this,
    /// so the server must do it. Stronger than "the client contains this code" because it removes
    /// the possibility that the code path is dead.
    /// </summary>
    ClientMandated = 2,

    /// <summary>Structure read out of the official client's own source.</summary>
    ClientCode = 3,

    /// <summary>
    /// Two or more independent non-Vortex implementations agreeing. Vortex agreeing never counts
    /// towards this: Vortex was in part written by reading those implementations, so it is not an
    /// independent observation of them.
    /// </summary>
    MultiImplementation = 4,

    /// <summary>A single third-party reference emulator.</summary>
    ReferenceEmulator = 5,

    /// <summary>What this repository's emulator currently does. Evidence, never authority.</summary>
    VortexEmulator = 6,

    /// <summary>A conclusion derived from other evidence rather than observed directly.</summary>
    Inference = 7,

    /// <summary>An unbacked guess. Recorded so it can be argued with, never treated as fact.</summary>
    Assumption = 8,
}
