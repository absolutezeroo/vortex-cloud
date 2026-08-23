using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Help;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// Reporting a guild-forum thread. The packet names the thread, not a player, so the author is
/// resolved from the forum grain — which also scopes the lookup to the group the client claimed,
/// so a crafted group id cannot be used to read threads elsewhere in the hotel.
/// </summary>
public class CallForHelpFromForumThreadMessageHandler(
    IGrainFactory grainFactory,
    ICfhTicketService tickets
) : IMessageHandler<CallForHelpFromForumThreadMessage>
{
    public async ValueTask HandleAsync(
        CallForHelpFromForumThreadMessage message,
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
            .GetThreadAuthorAsync(message.ThreadId, ct)
            .ConfigureAwait(false);

        string description =
            $"{message.Message}\n\nReported forum thread {message.ThreadId} in guild {message.GroupId}.";

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
