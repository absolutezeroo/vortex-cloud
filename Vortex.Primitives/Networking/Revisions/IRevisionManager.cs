using System.Collections.Generic;

namespace Vortex.Primitives.Networking.Revisions;

public interface IRevisionManager
{
    public IDictionary<string, IRevision> Revisions { get; }
    public string DefaultRevisionId { get; }

    public IRevision? GetRevision(string revisionName);

    public void RegisterRevision(IRevision revision);

    /// <summary>Explicitly pins <see cref="DefaultRevisionId"/> to an already-registered revision,
    /// instead of leaving it to whichever revision happened to register first.</summary>
    public void SetDefault(string revisionId);
}
