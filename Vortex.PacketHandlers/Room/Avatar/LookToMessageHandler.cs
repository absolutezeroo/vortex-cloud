using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Room.Avatar;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.Room.Avatar;

public class LookToMessageHandler(IGrainFactory grainFactory) : IMessageHandler<LookToMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        LookToMessage message,
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
            .LookToAvatarAsync(ctx.AsActionContext(), message.X, message.Y, ct)
            .ConfigureAwait(false);
    }
}
