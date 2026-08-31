using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Events.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Orleans;

namespace Vortex.Fishing.Events;

// A fishing session runs on a timer nobody is watching, so the two ways it can be orphaned — the
// player walks out of the room, or disconnects — are handled here rather than trusted to the
// client's StopFishing, which an unclean disconnect never sends. The event pipeline isolates handler
// exceptions, so a failure to stop a session cannot break the leave or the disconnect itself.

/// <summary>
/// Ends the fishing session when the player leaves the room the spot is in.
/// </summary>
/// <remarks>
/// Unconditional: the grain returns immediately when no session is running, and asking it first
/// would cost the same round trip the stop does.
/// </remarks>
public sealed class FishingStopOnRoomLeaveHandler(IGrainFactory grainFactory)
    : IEventHandler<PlayerLeftRoomEvent>
{
    public async ValueTask HandleAsync(
        PlayerLeftRoomEvent e,
        EventContext ctx,
        CancellationToken ct
    ) => await grainFactory.GetFishingSessionGrain(e.PlayerId).StopAsync(ct).ConfigureAwait(false);
}

/// <summary>
/// Ends the fishing session when the player disconnects.
/// </summary>
/// <remarks>
/// The grain deactivating would end it too, but not promptly: an activation with a live timer stays
/// alive precisely because the timer keeps touching it, so a dropped connection could leave a
/// session fishing for minutes.
/// </remarks>
public sealed class FishingStopOnDisconnectHandler(IGrainFactory grainFactory)
    : IEventHandler<PlayerDisconnectedEvent>
{
    public async ValueTask HandleAsync(
        PlayerDisconnectedEvent e,
        EventContext ctx,
        CancellationToken ct
    ) => await grainFactory.GetFishingSessionGrain(e.PlayerId).StopAsync(ct).ConfigureAwait(false);
}
