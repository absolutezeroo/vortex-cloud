using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Messages.Incoming.GroupForums;
using Vortex.Primitives.Messages.Outgoing.Groupforums;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.GroupForums;

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
