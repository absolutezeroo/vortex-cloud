using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.FriendList.Enums;
using Turbo.Primitives.Messages.Incoming.FriendList;
using Turbo.Primitives.Messages.Outgoing.FriendList;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.FriendList;

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
            return;

        var directory = _grainFactory.GetPlayerDirectoryGrain();
        var targetId = await directory
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

        var grain = _grainFactory.GetMessengerGrain(ctx.PlayerId);
        var error = await grain
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
