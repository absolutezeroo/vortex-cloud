using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Messages.Incoming.Groupforums;
using Turbo.Primitives.Messages.Outgoing.Groupforums;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Groupforums;

public class PostMessageMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<PostMessageMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        PostMessageMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.GroupId <= 0)
        {
            return;
        }

        ForumPostResultSnapshot? result = await _grainFactory
            .GetGroupForumGrain(message.GroupId)
            .PostAsync(ctx.PlayerId, message.ThreadId, message.Title, message.Message, ct)
            .ConfigureAwait(false);

        if (result is null)
        {
            return;
        }

        if (result.IsNewThread && result.Thread is not null)
        {
            await ctx.SendComposerAsync(
                    new PostThreadMessageComposer
                    {
                        GroupId = result.GroupId,
                        Thread = result.Thread,
                    },
                    ct
                )
                .ConfigureAwait(false);
        }
        else if (result.Post is not null)
        {
            await ctx.SendComposerAsync(
                    new PostMessageMessageComposer
                    {
                        GroupId = result.GroupId,
                        ThreadId = result.ThreadId,
                        Post = result.Post,
                    },
                    ct
                )
                .ConfigureAwait(false);
        }
    }
}
