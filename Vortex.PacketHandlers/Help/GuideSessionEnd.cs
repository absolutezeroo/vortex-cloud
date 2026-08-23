using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Help.Grains;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Outgoing.Help;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// Closing a guide session. Two packets arrive here — the guide marking it resolved and the
/// requester cancelling — and they differ only in the reason each side is told.
/// </summary>
internal static class GuideSessionEnd
{
    /// <summary>The requester gave up. Never zero: the client reads zero as "your guide
    /// vanished".</summary>
    public const int ReasonRequesterCancelled = 1;

    /// <summary>The guide closed it, which is what puts the requester in front of the feedback
    /// form.</summary>
    public const int ReasonGuideResolved = 2;

    public static async Task CloseAsync(
        IGrainFactory grainFactory,
        int playerId,
        int reason,
        CancellationToken ct
    )
    {
        if (playerId <= 0)
        {
            return;
        }

        int partnerId = await grainFactory
            .GetGuideDirectoryGrain()
            .EndSessionAsync(playerId, ct)
            .ConfigureAwait(false);

        if (partnerId <= 0)
        {
            return;
        }

        // Only the other side is told. The one who closed it already moved their own window on, and
        // a second Ended would take them out of the feedback form they were just put into.
        await grainFactory
            .GetPlayerPresenceGrain(partnerId)
            .SendComposerAsync(new GuideSessionEndedMessageComposer { EndReason = reason })
            .ConfigureAwait(false);
    }
}
