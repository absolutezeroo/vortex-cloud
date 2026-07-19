using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Users;

public class SelectFavouriteHabboGroupMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SelectFavouriteHabboGroupMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        SelectFavouriteHabboGroupMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.GroupId <= 0)
        {
            return;
        }

        await _grainFactory
            .GetGroupDirectoryGrain()
            .SetFavouriteAsync(ctx.PlayerId, message.GroupId, true, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new GroupDetailsChangedMessageComposer { GroupId = message.GroupId },
                ct
            )
            .ConfigureAwait(false);
    }
}
