using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Moderation;
using Vortex.Protocol.Messages.Incoming.Help;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// The plain "report a user" entry point. Shares <see cref="CfhReportHelper"/> with the four
/// attachment-specific variants rather than filing its own ticket, so that topic validation, the
/// self-report guard, the acknowledgement and the push to on-duty moderators cannot drift between
/// the paths.
/// </summary>
public class CallForHelpMessageHandler(IGrainFactory grainFactory, ICfhTicketService tickets)
    : IMessageHandler<CallForHelpMessage>
{
    public async ValueTask HandleAsync(
        CallForHelpMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        List<(int UserId, string Text)> evidence =
        [
            .. message.Evidence.Select(e => (e.UserId, e.Text)),
        ];

        await CfhReportHelper
            .SubmitAsync(
                grainFactory,
                tickets,
                ctx.PlayerId,
                message.TopicId,
                message.ReportedUserId,
                message.RoomId > 0 ? message.RoomId : null,
                message.Message,
                evidence,
                ct
            )
            .ConfigureAwait(false);
    }
}
