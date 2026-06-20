using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Groupforums;
using Turbo.Primitives.Messages.Outgoing.Groupforums;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Groupforums;

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
