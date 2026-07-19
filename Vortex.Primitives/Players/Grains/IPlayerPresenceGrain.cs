using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans.Observers;

namespace Vortex.Primitives.Players.Grains;

public partial interface IPlayerPresenceGrain : IGrainWithIntegerKey
{
    public Task RegisterSessionObserverAsync(ISessionContextObserver observer);
    public Task UnregisterSessionObserverAsync(CancellationToken ct);
    public Task<bool> IsOnlineAsync(CancellationToken ct);
    public Task SendComposerAsync(IComposer composer);
    public Task SendComposerAsync(params IComposer[] composers);
}
