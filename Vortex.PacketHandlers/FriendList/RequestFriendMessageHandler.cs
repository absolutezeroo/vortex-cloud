using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.FriendList.Enums;
using Vortex.Primitives.FriendList.Grains;
using Vortex.Primitives.Messages.Incoming.FriendList;
using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Grains;

namespace Vortex.PacketHandlers.FriendList;

public class RequestFriendMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<RequestFriendMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        RequestFriendMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || string.IsNullOrWhiteSpace(message.PlayerName))
        {
            return;
        }

        IPlayerDirectoryGrain directory = _grainFactory.GetPlayerDirectoryGrain();
        PlayerId? targetId = await directory
            .GetPlayerIdAsync(message.PlayerName, ct)
            .ConfigureAwait(false);

        if (targetId is null)
        {
            await ctx.SendComposerAsync(
                    new MessengerErrorMessageComposer
                    {
                        ClientMessageId = 0,
                        ErrorCode = FriendListErrorCodeType.FriendRequestNotFound,
                    },
                    ct
                )
                .ConfigureAwait(false);
            return;
        }

        IMessengerGrain grain = _grainFactory.GetMessengerGrain(ctx.PlayerId);
        FriendListErrorCodeType? error = await grain
            .SendFriendRequestAsync(targetId.Value, message.PlayerName, ct)
            .ConfigureAwait(false);

        if (error.HasValue)
        {
            await ctx.SendComposerAsync(
                    new MessengerErrorMessageComposer
                    {
                        ClientMessageId = 0,
                        ErrorCode = error.Value,
                    },
                    ct
                )
                .ConfigureAwait(false);
        }
    }
}
