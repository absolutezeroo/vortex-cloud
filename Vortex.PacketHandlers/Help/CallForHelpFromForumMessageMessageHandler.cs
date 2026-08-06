using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Help;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Help;

/// <summary>Reporting a single guild-forum post. Same author resolution as the thread variant.</summary>
public class CallForHelpFromForumMessageMessageHandler(
    IGrainFactory grainFactory,
    ICfhTicketService tickets
) : IMessageHandler<CallForHelpFromForumMessageMessage>
{
    public async ValueTask HandleAsync(
        CallForHelpFromForumMessageMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (message.GroupId <= 0)
        {
            return;
        }

        int authorId = await grainFactory
            .GetGroupForumGrain(message.GroupId)
            .GetPostAuthorAsync(message.PostId, ct)
            .ConfigureAwait(false);

        string description =
            $"{message.Message}\n\nReported forum post {message.PostId} in thread "
            + $"{message.ThreadId}, guild {message.GroupId}.";

        await CfhReportHelper
            .SubmitAsync(
                grainFactory,
                tickets,
                ctx.PlayerId,
                message.TopicId,
                authorId,
                roomId: null,
                description,
                [],
                ct
            )
            .ConfigureAwait(false);
    }
}
