using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Messages.Incoming.GroupForums;
using Vortex.Primitives.Messages.Outgoing.Groupforums;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.GroupForums;

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
        {
            return;
        }

        ThreadMessagesPageSnapshot? page = await _grainFactory
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
        {
            return;
        }

        await ctx.SendComposerAsync(new ThreadMessagesMessageComposer { Page = page }, ct)
            .ConfigureAwait(false);
    }
}
