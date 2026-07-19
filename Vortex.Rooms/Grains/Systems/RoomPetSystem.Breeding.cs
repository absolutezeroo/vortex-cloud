using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Furniture;
using Vortex.Database.Entities.Pets;
using Vortex.Logging;
using Vortex.Primitives;
using Vortex.Primitives.Action;
using Vortex.Primitives.Messages.Outgoing.Inventory.Pets;
using Vortex.Primitives.Messages.Outgoing.Notifications;
using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Messages.Outgoing.Room.Pets;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Avatars;

namespace Vortex.Rooms.Grains.Systems;

public sealed partial class RoomPetSystem
{
    public async Task TogglePetBreedingPermissionAsync(
        ActionContext ctx,
        int petId,
        CancellationToken ct
    )
    {
        await EnsurePetsLoadedAsync(ct).ConfigureAwait(false);

        if (!_roomGrain._state.PetsById.TryGetValue(petId, out PetSnapshot? pet))
        {
            return;
        }

        if (pet.OwnerId != ctx.PlayerId)
        {
            return;
        }

        PetSnapshot updated = pet with { CanBreed = !pet.CanBreed };
        _roomGrain._state.PetsById[petId] = updated;

        if (_motionByPetId.TryGetValue(petId, out PetMotionState? motion))
        {
            motion.IsStatsDirty = true;
        }
    }

    public async Task<bool> BreedPetsAsync(
        ActionContext ctx,
        int petOneId,
        int petTwoId,
        CancellationToken ct
    )
    {
        await EnsurePetsLoadedAsync(ct).ConfigureAwait(false);

        if (
            !_roomGrain._state.PetsById.TryGetValue(petOneId, out PetSnapshot? petOne)
            || !_roomGrain._state.PetsById.TryGetValue(petTwoId, out PetSnapshot? petTwo)
        )
        {
            await SendBreedingFailureAsync(ctx.PlayerId, 3, ct).ConfigureAwait(false);
            return false;
        }

        if (petOne.Type != petTwo.Type)
        {
            await SendBreedingFailureAsync(ctx.PlayerId, 1, ct).ConfigureAwait(false);
            return false;
        }

        if (!petTwo.CanBreed)
        {
            await SendBreedingFailureAsync(ctx.PlayerId, 2, ct).ConfigureAwait(false);
            return false;
        }

        int proposedRace = Random.Shared.Next(0, 2) == 0 ? petOne.Race : petTwo.Race;
        string proposedColor = BlendColors(petOne.Color, petTwo.Color);
        int proposedGender = Random.Shared.Next(0, 2);

        PendingBreedingSession session = new(
            petOneId,
            petTwoId,
            petOne.OwnerId,
            petTwo.OwnerId,
            proposedRace,
            proposedColor,
            proposedGender
        );

        _breedingByPetOneId[petOneId] = session;

        PetBreedingEventMessageComposer requesterMsg = new()
        {
            PetOneId = petOneId,
            PetTwoId = petTwoId,
            OwnerOneId = petOne.OwnerId.Value,
            OwnerTwoId = petTwo.OwnerId.Value,
            ProposedRace = proposedRace,
            ProposedColor = proposedColor,
            ProposedGender = proposedGender,
        };

        ConfirmBreedingRequestEventMessageComposer targetMsg = new()
        {
            PetOneId = petOneId,
            PetTwoId = petTwoId,
            OwnerOneId = petOne.OwnerId.Value,
            OwnerTwoId = petTwo.OwnerId.Value,
            ProposedRace = proposedRace,
            ProposedColor = proposedColor,
            ProposedGender = proposedGender,
        };

        await _roomGrain
            ._grainFactory.GetPlayerPresenceGrain(petOne.OwnerId)
            .SendComposerAsync(requesterMsg)
            .ConfigureAwait(false);

        await _roomGrain
            ._grainFactory.GetPlayerPresenceGrain(petTwo.OwnerId)
            .SendComposerAsync(targetMsg)
            .ConfigureAwait(false);

        return true;
    }

    public async Task<bool> ConfirmPetBreedingAsync(
        ActionContext ctx,
        int petId,
        CancellationToken ct
    )
    {
        PendingBreedingSession? session = _breedingByPetOneId.Values.FirstOrDefault(s =>
            s.PetTwoId == petId
        );

        if (session is null)
        {
            return false;
        }

        _breedingByPetOneId.Remove(session.PetOneId);

        await using VortexDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        PetEntity baby = new()
        {
            OwnerPlayerEntityId = session.OwnerOneId.Value,
            RoomEntityId = null,
            Name = "Baby",
            Type = _roomGrain._state.PetsById.TryGetValue(session.PetOneId, out PetSnapshot? p1)
                ? p1.Type
                : 0,
            Race = session.ProposedRace,
            Color = session.ProposedColor,
            Gender = session.ProposedGender == 0 ? AvatarGenderType.Male : AvatarGenderType.Female,
            Level = 1,
            Experience = 0,
            Energy = _roomGrain._roomConfig.Pet.EnergyCap,
            Nutrition = _roomGrain._roomConfig.Pet.NutritionCap,
            Respect = 0,
            X = 0,
            Y = 0,
            Z = 0,
            Direction = (int)Rotation.South,
            ParentOneId = session.PetOneId,
            ParentTwoId = session.PetTwoId,
            OwnerPlayerEntity = null!,
        };

        dbCtx.Pets.Add(baby);
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        ConfirmBreedingResultEventMessageComposer resultMsg = new()
        {
            Success = true,
            NewPetId = baby.Id,
        };

        await _roomGrain
            ._grainFactory.GetPlayerPresenceGrain(session.OwnerOneId)
            .SendComposerAsync(resultMsg)
            .ConfigureAwait(false);

        await _roomGrain
            ._grainFactory.GetPlayerPresenceGrain(session.OwnerTwoId)
            .SendComposerAsync(resultMsg)
            .ConfigureAwait(false);

        await _roomGrain
            ._grainFactory.GetPlayerPresenceGrain(session.OwnerOneId)
            .SendComposerAsync(new NestBreedingSuccessEventMessageComposer { NewPetId = baby.Id })
            .ConfigureAwait(false);

        await _roomGrain
            .SendComposerToRoomAsync(
                new PetBreedingResultEventMessageComposer
                {
                    PetOneId = session.PetOneId,
                    PetTwoId = session.PetTwoId,
                    Result = 0,
                }
            )
            .ConfigureAwait(false);

        return true;
    }

    public Task CancelPetBreedingAsync(ActionContext ctx, int petId, CancellationToken ct)
    {
        int keyToRemove = -1;

        foreach (KeyValuePair<int, PendingBreedingSession> kvp in _breedingByPetOneId)
        {
            if (kvp.Key == petId || kvp.Value.PetTwoId == petId)
            {
                keyToRemove = kvp.Key;
                break;
            }
        }

        if (keyToRemove >= 0)
        {
            _breedingByPetOneId.Remove(keyToRemove);
        }

        return Task.CompletedTask;
    }

    private static string BlendColors(string colorA, string colorB)
    {
        if (colorA.Length != 6 || colorB.Length != 6)
        {
            return colorA;
        }

        char[] result = new char[6];

        for (int i = 0; i < 6; i++)
        {
            result[i] = i % 2 == 0 ? colorA[i] : colorB[i];
        }

        return new string(result);
    }

    private async Task SendBreedingFailureAsync(PlayerId playerId, int reason, CancellationToken ct)
    {
        await _roomGrain
            ._grainFactory.GetPlayerPresenceGrain(playerId)
            .SendComposerAsync(new GoToBreedingNestFailureEventMessageComposer { Reason = reason })
            .ConfigureAwait(false);
    }

    public async Task<PetSnapshot?> PlantMonsterplantSeedAsync(
        ActionContext ctx,
        RoomObjectId seedItemId,
        CancellationToken ct
    )
    {
        await EnsureRoomReadyForPetPlacementAsync(ct).ConfigureAwait(false);

        if (!_roomGrain._state.ItemsById.TryGetValue(seedItemId, out IRoomItem? seedItem))
        {
            return null;
        }

        int x = seedItem.X;
        int y = seedItem.Y;
        double z = seedItem.Z;
        PlayerId ownerId = seedItem.OwnerId;

        await using VortexDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        PetEntity plant = new()
        {
            OwnerPlayerEntityId = ownerId.Value,
            OwnerPlayerEntity = null!,
            RoomEntityId = _roomGrain.RoomId.Value,
            Name = "Monsterplant",
            Type = MonsterplantPetType,
            Race = 0,
            Color = "ffffff",
            Gender = AvatarGenderType.Male,
            Level = 1,
            Experience = 0,
            Energy = _roomGrain._roomConfig.Pet.EnergyCap,
            Nutrition = _roomGrain._roomConfig.Pet.NutritionCap,
            Respect = 0,
            RarityLevel = Random.Shared.Next(1, 8),
            LastWateredAt = DateTime.UtcNow,
            X = x,
            Y = y,
            Z = z,
            Direction = (int)Rotation.South,
        };

        dbCtx.Pets.Add(plant);

        FurnitureEntity seedEntity = new() { Id = seedItemId.Value };
        dbCtx.Attach(seedEntity);
        seedEntity.DeletedAt = DateTime.UtcNow;
        dbCtx.Entry(seedEntity).Property(f => f.DeletedAt).IsModified = true;

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        PetSnapshot snapshot = RoomPetRuntime.ToSnapshot(plant);
        _roomGrain._state.PetsById[plant.Id] = snapshot;

        await SendPetAddedAsync(snapshot, ct).ConfigureAwait(false);

        await _roomGrain
            .ObjectModule.RemoveObjectAsync(ctx, seedItem, ct, ownerId)
            .ConfigureAwait(false);

        return snapshot;
    }
}
