using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.FriendList.Grains;
using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;

namespace Vortex.PacketHandlers.Users;

public class UnblockUserMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<UnblockUserMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        UnblockUserMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        IMessengerGrain grain = _grainFactory.GetMessengerGrain(ctx.PlayerId);
        await grain.UnblockUserAsync(PlayerId.Parse(message.PlayerId), ct).ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new BlockUserUpdateMessageComposer
                {
                    PlayerId = message.PlayerId,
                    IsBlocked = false,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
