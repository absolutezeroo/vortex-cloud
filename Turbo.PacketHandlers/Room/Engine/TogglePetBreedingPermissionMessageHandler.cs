using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Room.Engine;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.PacketHandlers.Room.Engine;

public class TogglePetBreedingPermissionMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<TogglePetBreedingPermissionMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        TogglePetBreedingPermissionMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.PetId <= 0)
        {
            return;
        }

        IRoomGrain room = _grainFactory.GetRoomGrain(ctx.RoomId);

        await room.TogglePetBreedingPermissionAsync(ctx.AsActionContext(), message.PetId, ct)
            .ConfigureAwait(false);
    }
}
