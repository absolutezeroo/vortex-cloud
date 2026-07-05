using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Help;
using Turbo.Primitives.Messages.Outgoing.Help;
using Turbo.Primitives.Moderation;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Help;

public class CallForHelpMessageHandler(IGrainFactory grainFactory, ICfhTicketService tickets)
    : IMessageHandler<CallForHelpMessage>
{
    public async ValueTask HandleAsync(
        CallForHelpMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.ReportedUserId <= 0 || message.TopicId <= 0)
        {
            return;
        }

        CfhTopicSnapshot? topic = await tickets
            .GetTopicAsync(message.TopicId, ct)
            .ConfigureAwait(false);

        if (topic is null)
        {
            return;
        }

        List<(int UserId, string Text)> evidence = message
            .Evidence.Select(e => (e.UserId, e.Text))
            .ToList();

        await tickets
            .CreateTicketAsync(
                message.TopicId,
                ctx.PlayerId,
                message.ReportedUserId,
                message.RoomId > 0 ? message.RoomId : null,
                message.Message,
                evidence,
                ct
            )
            .ConfigureAwait(false);

        await grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(
                new CallForHelpReplyMessageComposer
                {
                    Message = "Your report has been sent to our moderation team. Thank you.",
                }
            )
            .ConfigureAwait(false);
    }
}
