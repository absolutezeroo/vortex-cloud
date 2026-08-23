using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Room.Chat;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.Room.Chat;

public class CancelTypingMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<CancelTypingMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        CancelTypingMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        IRoomAvatars roomGrain = _grainFactory.GetRoomAvatars(ctx.RoomId);
        await roomGrain
            .SetAvatarTypingAsync(ctx.AsActionContext(), false, ct)
            .ConfigureAwait(false);
    }
}
