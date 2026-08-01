namespace Vortex.Revisions.Configuration;

/// <summary>
/// Which registered <see cref="Vortex.Primitives.Networking.Revisions.IRevision"/> new sessions are
/// pinned to. Left unset, the manager keeps its "first one registered wins" fallback - fine while
/// only one revision exists, but explicit once a second is added.
/// </summary>
public sealed class RevisionConfig
{
    public const string SECTION_NAME = "Vortex:Revisions";

    public string? DefaultRevisionId { get; init; }
}
