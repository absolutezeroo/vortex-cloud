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

namespace Vortex.PacketHandlers.FriendList;

public class SendMsgMessageHandler(IGrainFactory grainFactory) : IMessageHandler<SendMsgMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        SendMsgMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        IMessengerGrain grain = _grainFactory.GetMessengerGrain(ctx.PlayerId);
        PlayerId receiverId = PlayerId.Parse(message.ChatId);

        InstantMessageErrorCodeType? error = await grain
            .SendMessageAsync(
                receiverId,
                message.Message,
                message.ChatId,
                message.ConfirmationId,
                ct
            )
            .ConfigureAwait(false);

        if (error.HasValue)
        {
            await ctx.SendComposerAsync(
                    new InstantMessageErrorMessageComposer
                    {
                        ErrorCode = error.Value,
                        PlayerId = message.ChatId,
                        Message = message.Message,
                    },
                    ct
                )
                .ConfigureAwait(false);
        }
    }
}
