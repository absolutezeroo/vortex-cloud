using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Networking.Revisions;

namespace Vortex.Networking.Revisions;

public sealed class RevisionManager(ILogger<RevisionManager> logger) : IRevisionManager
{
    private readonly ILogger<RevisionManager> _logger = logger;

    public IDictionary<string, IRevision> Revisions { get; } = new Dictionary<string, IRevision>();
    public string DefaultRevisionId { get; private set; } = string.Empty;

    public IRevision? GetRevision(string revisionId) =>
        Revisions.TryGetValue(revisionId, out IRevision? revision) ? revision : null;

    public void RegisterRevision(IRevision revision)
    {
        if (revision is null)
        {
            return;
        }

        _logger.LogInformation("Revision Registered: {Revision}", revision.Revision);

        Revisions[revision.Revision] = revision;

        if (string.IsNullOrEmpty(DefaultRevisionId))
        {
            DefaultRevisionId = revision.Revision;
        }
    }
}
