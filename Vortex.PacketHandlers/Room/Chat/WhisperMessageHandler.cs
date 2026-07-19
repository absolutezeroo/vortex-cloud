using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Chat;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.Room.Chat;

public class WhisperMessageHandler : IMessageHandler<WhisperMessage>
{
    private readonly IGrainFactory _grainFactory;

    public WhisperMessageHandler(IGrainFactory grainFactory) => _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        WhisperMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (
            ctx is null
            || ctx.PlayerId <= 0
            || ctx.RoomId <= 0
            || string.IsNullOrWhiteSpace(message.Text)
            || string.IsNullOrWhiteSpace(message.RecipientName)
        )
        {
            return;
        }

        PlayerId? targetPlayerId = await _grainFactory
            .GetPlayerDirectoryGrain()
            .GetPlayerIdAsync(message.RecipientName, ct)
            .ConfigureAwait(false);

        if (targetPlayerId is null || targetPlayerId == ctx.PlayerId)
        {
            return;
        }

        IRoomGrain roomGrain = _grainFactory.GetRoomGrain(ctx.RoomId);

        await roomGrain
            .SendChatFromPlayerAsync(
                ctx.PlayerId,
                message.Text,
                (AvatarGestureType)0,
                message.StyleId,
                [],
                0,
                targetPlayerId
            )
            .ConfigureAwait(false);
    }
}
