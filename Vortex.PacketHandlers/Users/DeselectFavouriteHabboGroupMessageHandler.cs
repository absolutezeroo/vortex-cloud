using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Users;
using Vortex.Protocol.Messages.Outgoing.Users;

namespace Vortex.PacketHandlers.Users;

public class DeselectFavouriteHabboGroupMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<DeselectFavouriteHabboGroupMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        DeselectFavouriteHabboGroupMessage message,
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
            .SetFavouriteAsync(ctx.PlayerId, message.GroupId, false, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new GroupDetailsChangedMessageComposer { GroupId = message.GroupId },
                ct
            )
            .ConfigureAwait(false);
    }
}
