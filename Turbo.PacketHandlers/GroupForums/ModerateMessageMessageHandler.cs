using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Groupforums;
using Turbo.Primitives.Messages.Outgoing.Groupforums;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Groupforums;

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
            return;

        var post = await _grainFactory
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
            return;

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
