using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Engine;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.Room.Engine;

public class RemoveSaddleFromPetMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<RemoveSaddleFromPetMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        RemoveSaddleFromPetMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.PetId <= 0)
        {
            return;
        }

        IRoomGrain room = _grainFactory.GetRoomGrain(ctx.RoomId);

        await room.RemoveSaddleFromPetAsync(ctx.AsActionContext(), message.PetId, ct)
            .ConfigureAwait(false);
    }
}
