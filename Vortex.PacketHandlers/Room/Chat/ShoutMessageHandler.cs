using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Chat;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.Room.Chat;

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
