using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Groupforums;
using Turbo.Primitives.Messages.Outgoing.Groupforums;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Groupforums;

public class GetForumsListMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetForumsListMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetForumsListMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
            return;

        var page = await _grainFactory
            .GetGroupDirectoryGrain()
            .GetForumsListAsync(
                ctx.PlayerId,
                message.ListCode,
                message.StartIndex,
                message.Amount,
                ct
            )
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(new ForumsListMessageComposer { Page = page }, ct)
            .ConfigureAwait(false);
    }
}
