using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Events.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Orleans;

namespace Vortex.Players.Events;

/// <summary>
/// Takes a departing moderator off the CFH queue's broadcast list.
/// </summary>
/// <remarks>
/// The queue grain drops a subscriber whose delivery throws, but that only fires once something is
/// actually broadcast — on a quiet hotel a moderator who logged out hours ago would still be on the
/// list, and every ticket transition would pay a grain call to find that out. This retires them at
/// the moment they leave instead.
/// <para>
/// Hung off the disconnect event for the same reason the guide cleanup is: the session gateway has
/// no business knowing the moderation queue exists, and this seam already carries "somebody left".
/// Called for every player, moderator or not — unsubscribing someone who was never subscribed is a
/// set removal that misses, which is cheaper than asking first.
/// </para>
/// </remarks>
public sealed class ModerationQueueDisconnectHandler(IGrainFactory grainFactory)
    : IEventHandler<PlayerDisconnectedEvent>
{
    public async ValueTask HandleAsync(
        PlayerDisconnectedEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        if (e.PlayerId <= 0)
        {
            return;
        }

        await grainFactory
            .GetModerationQueueGrain()
            .UnsubscribeAsync(e.PlayerId)
            .ConfigureAwait(false);
    }
}
