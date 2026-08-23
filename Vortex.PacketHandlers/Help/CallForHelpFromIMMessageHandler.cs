using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Moderation;
using Vortex.Protocol.Messages.Incoming.Help;

namespace Vortex.PacketHandlers.Help;

/// <summary>Reporting someone from a private conversation: the same ticket as a room report, with
/// no room attached — the evidence is the IM buffer the reporter selected.</summary>
public class CallForHelpFromIMMessageHandler(IGrainFactory grainFactory, ICfhTicketService tickets)
    : IMessageHandler<CallForHelpFromIMMessage>
{
    public async ValueTask HandleAsync(
        CallForHelpFromIMMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        List<(int UserId, string Text)> evidence = message
            .Evidence.Select(line => (line.UserId, line.Text))
            .ToList();

        await CfhReportHelper
            .SubmitAsync(
                grainFactory,
                tickets,
                ctx.PlayerId,
                message.TopicId,
                message.ReportedUserId,
                roomId: null,
                message.Message,
                evidence,
                ct
            )
            .ConfigureAwait(false);
    }
}
