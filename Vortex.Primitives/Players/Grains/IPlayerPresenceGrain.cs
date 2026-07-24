using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans.Observers;

namespace Vortex.Primitives.Players.Grains;

public partial interface IPlayerPresenceGrain : IGrainWithIntegerKey
{
    public Task RegisterSessionObserverAsync(ISessionContextObserver observer);
    public Task UnregisterSessionObserverAsync(CancellationToken ct);
    public Task<bool> IsOnlineAsync(CancellationToken ct);

    // Interleaved on purpose. Rooms push composers at a player from inside calls that the player's own
    // presence grain made (entering/leaving a room), so a non-interleaved send would sit behind that
    // in-flight request forever: the presence grain is blocked awaiting the room, the room is blocked
    // awaiting the presence grain, and both only unwedge on the 30s Orleans call timeout. The handlers
    // are safe to interleave because they only enqueue onto the outgoing queue synchronously — no
    // awaits, so no other turn can observe a half-applied state change.
    [AlwaysInterleave]
    public Task SendComposerAsync(IComposer composer);

    [AlwaysInterleave]
    public Task SendComposerAsync(params IComposer[] composers);
}
