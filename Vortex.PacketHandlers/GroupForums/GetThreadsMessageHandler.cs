using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Messages.Incoming.GroupForums;
using Vortex.Primitives.Messages.Outgoing.Groupforums;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.GroupForums;

public class GetThreadsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetThreadsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetThreadsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.GroupId <= 0)
        {
            return;
        }

        ForumThreadsPageSnapshot? page = await _grainFactory
            .GetGroupForumGrain(message.GroupId)
            .GetThreadsAsync(ctx.PlayerId, message.StartIndex, message.Amount, ct)
            .ConfigureAwait(false);

        if (page is null)
        {
            return;
        }

        await ctx.SendComposerAsync(new ForumThreadsMessageComposer { Page = page }, ct)
            .ConfigureAwait(false);
    }
}
