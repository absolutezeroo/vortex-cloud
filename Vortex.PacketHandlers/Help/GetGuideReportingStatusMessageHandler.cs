using System;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Help;
using Vortex.Primitives.Help.Grains;
using Vortex.Primitives.Messages.Incoming.Help;
using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Players.Grains;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// "Do I already have something open with a guide?", asked before the help window opens.
/// </summary>
/// <remarks>
/// The reply is what opens the window: status 0 tells the client to show the new-help flow, so
/// answering nothing — which is what this did — left the Help button doing nothing at all.
/// </remarks>
public class GetGuideReportingStatusMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetGuideReportingStatusMessage>
{
    /// <summary>Nothing pending: open the new-help window.</summary>
    private const int StatusNothingPending = 0;

    /// <summary>A session is under way: show it instead.</summary>
    private const int StatusPendingTicket = 1;

    /// <summary>The plainest ticket shape — the other party and nothing more. The richer types add
    /// a description or a room name, neither of which a live session has to hand.</summary>
    private const int PlainTicketType = 0;

    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetGuideReportingStatusMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        GuideSessionSnapshot? session = await _grainFactory
            .GetGuideDirectoryGrain()
            .GetSessionAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (session is null)
        {
            await ctx.SendComposerAsync(
                    new GuideReportingStatusMessageComposer { StatusCode = StatusNothingPending },
                    ct
                )
                .ConfigureAwait(false);
            return;
        }

        bool isGuide = session.GuideId == ctx.PlayerId;
        int otherPartyId = isGuide ? session.RequesterId : session.GuideId;

        PlayerSummarySnapshot other = await _grainFactory
            .GetPlayerGrain(otherPartyId)
            .GetSummaryAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new GuideReportingStatusMessageComposer
                {
                    StatusCode = StatusPendingTicket,
                    PendingTicket = new GuidePendingTicket
                    {
                        TicketType = PlainTicketType,
                        SecondsAgo = 0,
                        IsGuide = isGuide,
                        OtherPartyName = other.Name,
                        OtherPartyFigure = other.Figure,
                    },
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
