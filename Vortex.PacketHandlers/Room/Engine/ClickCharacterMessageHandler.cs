using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Engine;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.Room.Engine;

public class ClickCharacterMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<ClickCharacterMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        ClickCharacterMessage message,
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
            .ClickCharacterAsync(ctx.AsActionContext(), message.ObjectId, ct)
            .ConfigureAwait(false);
    }
}
