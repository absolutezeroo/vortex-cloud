using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Room.Chat;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.PacketHandlers.Room.Chat;

public class ShoutMessageHandler : IMessageHandler<ShoutMessage>
{
    private readonly IGrainFactory _grainFactory;

    public ShoutMessageHandler(IGrainFactory grainFactory) => _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        ShoutMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (
            ctx is null
            || ctx.PlayerId <= 0
            || ctx.RoomId <= 0
            || string.IsNullOrWhiteSpace(message.Text)
        )
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
                0
            )
            .ConfigureAwait(false);
    }
}
