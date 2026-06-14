using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.FriendList;
using Turbo.Primitives.Messages.Outgoing.FriendList;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players;

namespace Turbo.PacketHandlers.FriendList;

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
            return;

        var grain = _grainFactory.GetMessengerGrain(ctx.PlayerId);
        var receiverId = PlayerId.Parse(message.ChatId);

        var error = await grain.SendMessageAsync(
            receiverId,
            message.Message,
            message.ChatId,
            message.ConfirmationId,
            ct
        ).ConfigureAwait(false);

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
            ).ConfigureAwait(false);
        }
    }
}
