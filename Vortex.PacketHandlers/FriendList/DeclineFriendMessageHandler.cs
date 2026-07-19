using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.FriendList.Grains;
using Vortex.Primitives.Messages.Incoming.FriendList;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.FriendList;

public class DeclineFriendMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<DeclineFriendMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        DeclineFriendMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        IMessengerGrain grain = _grainFactory.GetMessengerGrain(ctx.PlayerId);

        await grain
            .DeclineFriendRequestsAsync(message.Friends ?? [], message.DeclineAll, ct)
            .ConfigureAwait(false);
    }
}
