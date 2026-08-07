using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Events.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Help.Grains;
using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Orleans;

namespace Vortex.Players.Events;

/// <summary>
/// Takes a disconnecting player out of the guide subsystem.
/// </summary>
/// <remarks>
/// Being on duty means available right now, and a closed client is the loudest way of saying you are
/// not. Left in, a departed guide keeps being offered requests and never answers any of them — and
/// because a request only moves on when it is declined, it sits in front of somebody who is gone
/// while the player who asked waits for an answer that cannot come. The counters lie to everyone
/// else at the same time.
/// <para>
/// A live session ends the same way, so the partner is told rather than left talking to a window
/// nobody is behind. A chat review the player was holding is released too, which can be the thing
/// that finally lets it resolve for the guardians who did vote.
/// </para>
/// <para>
/// Hung off the disconnect event rather than called from the session gateway: the networking layer
/// has no business knowing the guide subsystem exists, and this is the seam that already carries
/// "somebody left" to everyone who cares.
/// </para>
/// </remarks>
public sealed class GuideDisconnectCleanupHandler(IGrainFactory grainFactory)
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

        IGuideDirectoryGrain guides = grainFactory.GetGuideDirectoryGrain();

        await guides.ClearDutyAsync(e.PlayerId, ct).ConfigureAwait(false);

        int partnerId = await guides.EndSessionAsync(e.PlayerId, ct).ConfigureAwait(false);

        if (partnerId > 0)
        {
            // Zero is the one reason that means "the other side is simply gone", and this is the
            // only place that can honestly send it: every other way a session ends is somebody
            // choosing to end it. The partner would otherwise sit in a conversation with nobody in
            // it, with no way to tell.
            await grainFactory
                .GetPlayerPresenceGrain(partnerId)
                .SendComposerAsync(new GuideSessionEndedMessageComposer { EndReason = 0 })
                .ConfigureAwait(false);
        }

        await guides.ChatReviewDetachAsync(e.PlayerId, ct).ConfigureAwait(false);
    }
}
