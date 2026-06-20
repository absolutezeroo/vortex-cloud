using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Messages.Incoming.GroupForums;
using Turbo.Primitives.Messages.Outgoing.Groupforums;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.GroupForums;

public class GetForumStatsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetForumStatsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetForumStatsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.GroupId <= 0)
        {
            return;
        }

        ForumSnapshot? forum = await _grainFactory
            .GetGroupForumGrain(message.GroupId)
            .GetForumAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (forum is null)
        {
            return;
        }

        await ctx.SendComposerAsync(new ForumDataMessageComposer { Forum = forum }, ct)
            .ConfigureAwait(false);
    }
}
