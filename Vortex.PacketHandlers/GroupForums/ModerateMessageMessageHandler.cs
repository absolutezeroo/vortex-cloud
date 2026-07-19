using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Messages.Incoming.GroupForums;
using Vortex.Primitives.Messages.Outgoing.Groupforums;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.GroupForums;

public class ModerateMessageMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<ModerateMessageMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        ModerateMessageMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.GroupId <= 0)
        {
            return;
        }

        ForumPostSnapshot? post = await _grainFactory
            .GetGroupForumGrain(message.GroupId)
            .ModerateMessageAsync(
                ctx.PlayerId,
                message.ThreadId,
                message.MessageId,
                message.Action,
                ct
            )
            .ConfigureAwait(false);

        if (post is null)
        {
            return;
        }

        await ctx.SendComposerAsync(
                new UpdateMessageMessageComposer
                {
                    GroupId = message.GroupId,
                    ThreadId = message.ThreadId,
                    Post = post,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
