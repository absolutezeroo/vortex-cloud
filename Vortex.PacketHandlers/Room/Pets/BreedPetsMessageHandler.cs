using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Protocol.Messages.Incoming.Room.Pets;

namespace Vortex.PacketHandlers.Room.Pets;

public class BreedPetsMessageHandler(IGrainFactory grainFactory) : IMessageHandler<BreedPetsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        BreedPetsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.PetOneId <= 0 || message.PetTwoId <= 0)
        {
            return;
        }

        IRoomPets room = _grainFactory.GetRoomPets(ctx.RoomId);
        ActionContext actorCtx = ctx.AsActionContext();

        switch (message.Action)
        {
            case PetBreedingActionType.Request:
                await room.BreedPetsAsync(actorCtx, message.PetOneId, message.PetTwoId, ct)
                    .ConfigureAwait(false);
                break;

            case PetBreedingActionType.Cancel:
                await room.CancelPetBreedingAsync(actorCtx, message.PetOneId, ct)
                    .ConfigureAwait(false);
                break;

            // Accepting only opens the naming dialog on the accepting client; the session is
            // already pending and ConfirmPetBreeding is what completes it. Re-running the request
            // here would notify both owners a second time.
            case PetBreedingActionType.Accept:
                break;
        }
    }
}
