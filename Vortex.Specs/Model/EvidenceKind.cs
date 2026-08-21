namespace Vortex.Specs.Model;

/// <summary>
/// What kind of artefact a piece of evidence was read out of. Kept separate from
/// <see cref="EvidenceAuthority"/> because the same kind can carry different weight depending on
/// which tree it came from (a composer in the official client outranks one in a fan client).
/// </summary>
public enum EvidenceKind
{
    Unknown = 0,
    ClientComposer,
    ClientParser,
    ClientCallSite,
    ClientHeaderRegistry,
    EmulatorParser,
    EmulatorSerializer,
    EmulatorHeader,
    EmulatorHandler,
    EmulatorFlow,
    ReferenceHandler,
    ReferenceComposer,
    ReferenceHeader,
    Capture,
    Inference,
}
