using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.FriendList.Grains;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Protocol.Messages.Incoming.Users;
using Vortex.Protocol.Messages.Outgoing.Users;

namespace Vortex.PacketHandlers.Users;

public class BlockUserMessageHandler(IGrainFactory grainFactory) : IMessageHandler<BlockUserMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        BlockUserMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        IMessengerGrain grain = _grainFactory.GetMessengerGrain(ctx.PlayerId);
        await grain.BlockUserAsync(PlayerId.Parse(message.PlayerId), ct).ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new BlockUserUpdateMessageComposer
                {
                    PlayerId = message.PlayerId,
                    IsBlocked = true,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
