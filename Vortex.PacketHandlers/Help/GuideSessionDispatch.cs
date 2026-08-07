using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Help;
using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Players;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// Turns a matching outcome into packets. Both handlers produce the same three outcomes — offered,
/// paired, failed — and both have to reach a player who is not the one whose packet started it, so
/// the sending lives here rather than twice.
/// </summary>
/// <remarks>
/// Everything goes out through the target's presence grain: it is the only thing that knows whether
/// they are still connected, and it is how a handler reaches a session that is not its own.
/// </remarks>
internal static class GuideSessionDispatch
{
    public static async Task DeliverAsync(
        IGrainFactory grainFactory,
        GuideRequestOutcome outcome,
        CancellationToken ct
    )
    {
        if (outcome.Failed)
        {
            await SendAsync(
                    grainFactory,
                    outcome.RequesterId,
                    new GuideSessionErrorMessageComposer { ErrorCode = outcome.ErrorCode }
                )
                .ConfigureAwait(false);

            return;
        }

        if (outcome.Session is GuideSessionSnapshot session)
        {
            await SendStartedAsync(grainFactory, session, ct).ConfigureAwait(false);

            return;
        }

        if (outcome.OfferedGuideId > 0)
        {
            // Only the guide is told. The requester's client put itself in the waiting state when it
            // sent the request, and telling them again on every hand-off would flash their dialog
            // each time another guide declined.
            await SendAsync(
                    grainFactory,
                    outcome.OfferedGuideId,
                    new GuideSessionAttachedMessageComposer
                    {
                        AsGuide = true,
                        HelpRequestType = outcome.HelpRequestType,
                        HelpRequestDescription = outcome.Description,
                        RoleSpecificWaitTime = 0,
                    }
                )
                .ConfigureAwait(false);
        }
    }

    private static async Task SendStartedAsync(
        IGrainFactory grainFactory,
        GuideSessionSnapshot session,
        CancellationToken ct
    )
    {
        PlayerSummarySnapshot? requester = await grainFactory
            .GetPlayerGrain(session.RequesterId)
            .GetSummaryAsync(ct)
            .ConfigureAwait(false);

        PlayerSummarySnapshot? guide = await grainFactory
            .GetPlayerGrain(session.GuideId)
            .GetSummaryAsync(ct)
            .ConfigureAwait(false);

        // One packet naming both people, sent to both: each side reads the other out of it.
        GuideSessionStartedMessageComposer started = new()
        {
            RequesterId = session.RequesterId,
            RequesterName = requester?.Name ?? string.Empty,
            RequesterFigure = requester?.Figure ?? string.Empty,
            GuideId = session.GuideId,
            GuideName = guide?.Name ?? string.Empty,
            GuideFigure = guide?.Figure ?? string.Empty,
        };

        await SendAsync(grainFactory, session.RequesterId, started).ConfigureAwait(false);
        await SendAsync(grainFactory, session.GuideId, started).ConfigureAwait(false);
    }

    private static Task SendAsync(IGrainFactory grainFactory, int playerId, IComposer composer) =>
        playerId <= 0
            ? Task.CompletedTask
            : grainFactory.GetPlayerPresenceGrain(playerId).SendComposerAsync(composer);
}
