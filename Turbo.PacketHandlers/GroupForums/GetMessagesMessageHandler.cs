using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Groupforums;
using Turbo.Primitives.Messages.Outgoing.Groupforums;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Groupforums;

public class GetMessagesMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetMessagesMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetMessagesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.GroupId <= 0)
            return;

        var page = await _grainFactory
            .GetGroupForumGrain(message.GroupId)
            .GetMessagesAsync(
                ctx.PlayerId,
                message.ThreadId,
                message.StartIndex,
                message.Amount,
                ct
            )
            .ConfigureAwait(false);

        if (page is null)
            return;

        await ctx.SendComposerAsync(new ThreadMessagesMessageComposer { Page = page }, ct)
            .ConfigureAwait(false);
    }
}
