using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Messages.Incoming.GroupForums;
using Vortex.Primitives.Messages.Outgoing.Groupforums;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.GroupForums;

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
        {
            return;
        }

        ForumsListPageSnapshot page = await _grainFactory
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
