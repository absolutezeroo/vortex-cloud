using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Messages.Incoming.Groupforums;
using Turbo.Primitives.Messages.Outgoing.Groupforums;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Groupforums;

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
