using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Moderation;
using Vortex.Protocol.Messages.Incoming.Help;
using Vortex.Protocol.Messages.Outgoing.Callforhelp;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// "My reports", opened from the help window: what the player reported, and what came of it.
/// </summary>
/// <remarks>
/// Answered even when the player has never reported anyone. The client builds the window from the
/// reply, so silence here is the window not opening at all — which is what header 1834 did until
/// this handler existed, logged only as an unknown incoming packet.
/// </remarks>
public class GetMyCfhReportStatusMessageHandler(ICfhTicketService tickets)
    : IMessageHandler<GetMyCfhReportStatusMessage>
{
    public async ValueTask HandleAsync(
        GetMyCfhReportStatusMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        ImmutableArray<CfhReportStatusSnapshot> reports = await tickets
            .GetReportHistoryForReporterAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(new MyCfhReportStatusMessageComposer { Reports = reports }, ct)
            .ConfigureAwait(false);
    }
}
