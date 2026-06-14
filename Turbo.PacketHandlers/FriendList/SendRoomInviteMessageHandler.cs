using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.FriendList;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players;

namespace Turbo.PacketHandlers.FriendList;

public class SendRoomInviteMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SendRoomInviteMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        SendRoomInviteMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.FriendIds.Count == 0)
            return;

        // Fire-and-forget room invites to each friend's grain
        foreach (var friendId in message.FriendIds)
        {
            var friendGrain = _grainFactory.GetMessengerGrain(PlayerId.Parse(friendId));
            _ = friendGrain.ReceiveRoomInviteAsync(ctx.PlayerId, message.Message, CancellationToken.None);
        }

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
