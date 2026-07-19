using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.GroupForums;
using Vortex.Primitives.Messages.Outgoing.Groupforums;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.GroupForums;

public class GetUnreadForumsCountMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetUnreadForumsCountMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetUnreadForumsCountMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        int count = await _grainFactory
            .GetGroupDirectoryGrain()
            .GetUnreadForumsCountAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new UnreadForumsCountMessageComposer { UnreadForumsCount = count },
                ct
            )
            .ConfigureAwait(false);
    }
}
