using System;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Protocol.Messages.Incoming.Room.Pets;
using Vortex.Protocol.Messages.Outgoing.Room.Pets;

namespace Vortex.PacketHandlers.Room.Pets;

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

        IRoomPets room = _grainFactory.GetRoomPets(ctx.RoomId);
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

        const int monsterplantPetType = 16;
        const int monsterplantMaxWellBeingSeconds = 86_400;

        bool isPlant = pet.Type == monsterplantPetType;

        int remainingWellBeingSeconds = 0;
        if (isPlant && pet.LastWateredAt.HasValue)
        {
            remainingWellBeingSeconds = Math.Max(
                0,
                (int)(
                    monsterplantMaxWellBeingSeconds
                    - (DateTime.UtcNow - pet.LastWateredAt.Value).TotalSeconds
                )
            );
        }

        IPlayerPresenceGrain presence = _grainFactory.GetPlayerPresenceGrain(ctx.PlayerId);
        await presence
            .SendComposerAsync(
                new PetInfoMessageComposer
                {
                    Pet = pet,
                    OwnerName = ownerName,
                    Age = pet.AgeInDays(DateTime.UtcNow),
                    CanBreed = !isPlant && pet.CanBreed,
                    // Spent seed charge => no harvest button, see RoomPetRuntime.CanHarvest.
                    CanHarvest = isPlant && pet.Level >= 7 && pet.Energy > 0 && pet.CanBreed,
                    CanRevive = isPlant && pet.Energy == 0,
                    HasBreedingPermission = !isPlant && pet.CanBreed,
                    RarityLevel = pet.RarityLevel,
                    MaxWellBeingSeconds = isPlant ? monsterplantMaxWellBeingSeconds : 0,
                    RemainingWellBeingSeconds = remainingWellBeingSeconds,
                    RemainingGrowingSeconds = 0,
                }
            )
            .ConfigureAwait(false);
    }
}
