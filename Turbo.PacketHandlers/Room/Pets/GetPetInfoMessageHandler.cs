using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Room.Pets;
using Turbo.Primitives.Messages.Outgoing.Room.Pets;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Pets.Snapshots;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.PacketHandlers.Room.Pets;

public class GetPetInfoMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetPetInfoMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetPetInfoMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.PetId <= 0)
        {
            return;
        }

        IRoomGrain room = _grainFactory.GetRoomGrain(ctx.RoomId);
        PetSnapshot? pet = await room.GetPlacedPetSnapshotAsync(message.PetId, ct)
            .ConfigureAwait(false);

        if (pet is null)
        {
            return;
        }

        IPlayerDirectoryGrain directory = _grainFactory.GetPlayerDirectoryGrain();
        string ownerName = await directory
            .GetPlayerNameAsync(pet.OwnerId, ct)
            .ConfigureAwait(false);

        IPlayerPresenceGrain presence = _grainFactory.GetPlayerPresenceGrain(ctx.PlayerId);
        await presence
            .SendComposerAsync(
                new PetInfoMessageComposer
                {
                    Pet = pet,
                    OwnerName = ownerName,
                    CanBreed = pet.CanBreed,
                    HasBreedingPermission = pet.CanBreed,
                }
            )
            .ConfigureAwait(false);
    }
}
