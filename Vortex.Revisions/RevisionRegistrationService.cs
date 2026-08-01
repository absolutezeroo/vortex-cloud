using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Configuration;

namespace Vortex.Revisions;

/// <summary>
/// Registers every discovered <see cref="IRevision"/> with the <see cref="IRevisionManager"/> at
/// startup, by contract rather than by a fixed constructor parameter naming a concrete revision
/// type. Registered before <c>VortexEmulator</c> so sessions never see the network listeners open
/// before a default revision is set.
/// </summary>
public sealed class RevisionRegistrationService(
    IRevisionManager revisionManager,
    IEnumerable<IRevision> revisions,
    IOptions<RevisionConfig> config
) : IHostedService
{
    private readonly IRevisionManager _revisionManager = revisionManager;
    private readonly IEnumerable<IRevision> _revisions = revisions;
    private readonly RevisionConfig _config = config.Value;

    public Task StartAsync(CancellationToken ct)
    {
        foreach (IRevision revision in _revisions)
        {
            _revisionManager.RegisterRevision(revision);
        }

        if (!string.IsNullOrEmpty(_config.DefaultRevisionId))
        {
            _revisionManager.SetDefault(_config.DefaultRevisionId);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
