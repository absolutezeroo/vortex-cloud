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
    public async Task<PetSnapshot?> RespectPetAsync(
        ActionContext ctx,
        int petId,
        CancellationToken ct
    )
    {
        await EnsurePetsLoadedAsync(ct).ConfigureAwait(false);

        if (!_roomGrain._state.PetsById.TryGetValue(petId, out PetSnapshot? pet))
        {
            return null;
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (pet.RespectLastResetDate != today)
        {
            pet = pet with { RespectTodayCount = 0, RespectLastResetDate = today };
            _roomGrain._state.PetsById[petId] = pet;
        }

        int dailyCap = _roomGrain._roomConfig.Pet.RespectDailyCapPerPet;

        if (pet.RespectTodayCount >= dailyCap)
        {
            return pet;
        }

        int respectXp = _roomGrain._roomConfig.Pet.RespectXpReward;
        PetSnapshot updated = await GrantXpAndLevelUpAsync(pet, respectXp, ct)
            .ConfigureAwait(false);
        updated = updated with
        {
            Respect = updated.Respect + 1,
            RespectTodayCount = updated.RespectTodayCount + 1,
            RespectLastResetDate = today,
        };
        _roomGrain._state.PetsById[petId] = updated;

        if (_motionByPetId.TryGetValue(petId, out PetMotionState? motion))
        {
            motion.IsStatsDirty = true;
        }

        await SendPetUpdatedAsync(updated, ct).ConfigureAwait(false);
        await BroadcastPetRespectNotificationAsync(updated).ConfigureAwait(false);

        return updated;
    }

    public async Task<PetSnapshot?> GrantPetCommandXpAsync(
        ActionContext ctx,
        int petId,
        CancellationToken ct
    )
    {
        await EnsurePetsLoadedAsync(ct).ConfigureAwait(false);

        if (!_roomGrain._state.PetsById.TryGetValue(petId, out PetSnapshot? pet))
        {
            return null;
        }

        int commandXp = _roomGrain._roomConfig.Pet.CommandXpReward;
        PetSnapshot updated = await GrantXpAndLevelUpAsync(pet, commandXp, ct)
            .ConfigureAwait(false);
        _roomGrain._state.PetsById[petId] = updated;

        if (_motionByPetId.TryGetValue(petId, out PetMotionState? motion))
        {
            motion.IsStatsDirty = true;
        }

        await SendPetUpdatedAsync(updated, ct).ConfigureAwait(false);

        return updated;
    }

    public async Task<PetSnapshot?> GiveSupplementToPetAsync(
        ActionContext ctx,
        int petId,
        CancellationToken ct
    )
    {
        await EnsurePetsLoadedAsync(ct).ConfigureAwait(false);

        if (!_roomGrain._state.PetsById.TryGetValue(petId, out PetSnapshot? pet))
        {
            return null;
        }

        int energyCap = _roomGrain._petLevelProvider.GetEnergyCapForLevel(pet.Type, pet.Level);
        int newEnergy = Math.Min(
            pet.Energy + _roomGrain._roomConfig.Pet.SupplementEnergyBoost,
            energyCap
        );

        int supplementXp = _roomGrain._roomConfig.Pet.SupplementXpReward;
        PetSnapshot withEnergy = pet with { Energy = newEnergy };
        _roomGrain._state.PetsById[petId] = withEnergy;

        PetSnapshot updated = await GrantXpAndLevelUpAsync(withEnergy, supplementXp, ct)
            .ConfigureAwait(false);
        _roomGrain._state.PetsById[petId] = updated;

        bool petWokeUp = false;

        if (_motionByPetId.TryGetValue(petId, out PetMotionState? motion))
        {
            motion.IsStatsDirty = true;
            if (
                motion.IsSleeping
                && updated.Energy >= _roomGrain._roomConfig.Pet.SleepWakeEnergyThreshold
            )
            {
                motion.IsSleeping = false;
                motion.SleepPostureSent = false;
                petWokeUp = true;
            }
        }

        await SendPetUpdatedAsync(updated, ct).ConfigureAwait(false);

        if (petWokeUp)
        {
            await BroadcastPetVocalAsync(updated, "GENERIC_HAPPY").ConfigureAwait(false);
        }

        return updated;
    }

    private Task BroadcastPetVocalAsync(PetSnapshot pet, string vocalType)
    {
        return _roomGrain.SendComposerToRoomAsync(
            new PetVocalMessageComposer
            {
                PetObjectId = RoomPetRuntime.ToRoomObjectId(pet.PetId),
                PetType = pet.Type,
                VocalType = vocalType,
                VocalIndex = GetVocalIndex(pet.Type, vocalType),
            }
        );
    }

    private Task BroadcastPetRespectNotificationAsync(PetSnapshot pet)
    {
        return _roomGrain.SendComposerToRoomAsync(
            new PetRespectNotificationEventMessageComposer
            {
                PetRespect = pet.Respect,
                PetOwnerId = pet.OwnerId.Value,
                PetId = pet.PetId,
                PetName = pet.Name,
                PetType = pet.Type,
                PetColor = pet.Color,
                PetRace = pet.Race,
                PetLevel = pet.Level,
            }
        );
    }

    private string SelectVocalForState(PetSnapshot pet, PetMotionState motion)
    {
        if (motion.IsSleeping)
        {
            return "SLEEPING";
        }

        bool isHungry = pet.Nutrition < _roomGrain._roomConfig.Pet.HungerThreshold;
        bool isThirsty =
            pet.Energy < _roomGrain._roomConfig.Pet.ThirstThreshold
            && pet.Energy > _roomGrain._roomConfig.Pet.SleepWakeEnergyThreshold;
        bool isTired =
            pet.Energy > 0 && pet.Energy <= _roomGrain._roomConfig.Pet.SleepWakeEnergyThreshold;

        if (isHungry)
        {
            return "HUNGRY";
        }

        if (isThirsty)
        {
            return "THIRSTY";
        }

        if (isTired)
        {
            return "TIRED";
        }

        return Random.Shared.Next(0, 3) switch
        {
            0 => "GENERIC_NEUTRAL",
            1 => "GENERIC_HAPPY",
            _ => "PLAYFUL",
        };
    }

    private static int GetVocalIndex(int petType, string vocalType)
    {
        // Max is the exclusive upper bound; Random.Next(0, max) → indices 0..max-1
        int max = (petType, vocalType) switch
        {
            (35, "nlDISOBEY") => 3,
            (35, "DRINKING") => 2,
            (35, "EATING") => 3,
            (35, "GENERIC_HAPPY") => 3,
            (35, "GENERIC_NEUTRAL") => 3,
            (35, "GENERIC_SAD") => 2,
            (35, "GREET_OWNER") => 3,
            (35, "HUNGRY") => 4,
            (35, "LEVEL_UP") => 4,
            (35, "MUTED") => 1,
            (35, "PLAYFUL") => 2,
            (35, "PLAYING") => 2,
            (35, "SLEEPING") => 3,
            (35, "THIRSTY") => 3,
            (35, "TIRED") => 3,
            (35, "UNKNOWN_COMMAND") => 2,
            (15, "DISOBEY") => 3,
            (15, "DRINKING") => 2,
            (15, "EATING") => 3,
            (15, "GENERIC_HAPPY") => 3,
            (15, "GENERIC_NEUTRAL") => 3,
            (15, "GENERIC_SAD") => 2,
            (15, "GREET_OWNER") => 3,
            (15, "HUNGRY") => 4,
            (15, "LEVEL_UP") => 4,
            (15, "MUTED") => 1,
            (15, "PLAYFUL") => 2,
            (15, "PLAYING") => 2,
            (15, "SLEEPING") => 3,
            (15, "THIRSTY") => 3,
            (15, "TIRED") => 3,
            (15, "UNKNOWN_COMMAND") => 2,
            _ => 3,
        };
        return max > 1 ? Random.Shared.Next(0, max) : 0;
    }

    public async Task<PetSnapshot?> IssueCommandAsync(
        ActionContext ctx,
        int petId,
        int commandId,
        CancellationToken ct
    )
    {
        await EnsurePetsLoadedAsync(ct).ConfigureAwait(false);

        if (!_roomGrain._state.PetsById.TryGetValue(petId, out PetSnapshot? pet))
        {
            return null;
        }

        if (pet.OwnerId != ctx.PlayerId)
        {
            return null;
        }

        PetCommandEntry? cmd = _roomGrain._petCommandProvider.GetCommandConfig(pet.Type, commandId);

        if (cmd is null)
        {
            return null;
        }

        if (pet.Level < cmd.LevelRequired)
        {
            await BroadcastPetVocalAsync(pet, "UNKNOWN_COMMAND").ConfigureAwait(false);
            return null;
        }

        if (_motionByPetId.TryGetValue(petId, out PetMotionState? motion) && motion.IsSleeping)
        {
            await BroadcastPetVocalAsync(pet, "SLEEPING").ConfigureAwait(false);
            return null;
        }

        if (pet.Energy < cmd.EnergyCost)
        {
            await BroadcastPetVocalAsync(pet, "TIRED").ConfigureAwait(false);
            return null;
        }

        int newEnergy = pet.Energy - cmd.EnergyCost;
        PetSnapshot withEnergy = pet with { Energy = newEnergy };
        _roomGrain._state.PetsById[petId] = withEnergy;

        PetSnapshot updated = await GrantXpAndLevelUpAsync(withEnergy, cmd.XpReward, ct)
            .ConfigureAwait(false);
        _roomGrain._state.PetsById[petId] = updated;

        if (motion is not null)
        {
            motion.IsStatsDirty = true;

            if (newEnergy == 0)
            {
                motion.IsSleeping = true;
                motion.SleepPostureSent = false;
            }
        }

        if (!string.IsNullOrEmpty(cmd.Posture))
        {
            RoomPetAvatarSnapshot postureSnapshot = await ToAvatarSnapshotAsync(
                    updated,
                    $"/{cmd.Posture}/",
                    ct
                )
                .ConfigureAwait(false);

            await _roomGrain
                .SendComposerToRoomAsync(
                    new UserUpdateMessageComposer { Avatars = [postureSnapshot] }
                )
                .ConfigureAwait(false);
        }
        else
        {
            await SendPetUpdatedAsync(updated, ct).ConfigureAwait(false);
        }

        return updated;
    }

    private async Task<PetSnapshot> GrantXpAndLevelUpAsync(
        PetSnapshot pet,
        int xp,
        CancellationToken ct
    )
    {
        int newExperience = pet.Experience + xp;
        int newLevel = _roomGrain._petLevelProvider.GetLevelForExperience(pet.Type, newExperience);
        bool leveledUp = newLevel > pet.Level;
        int maxLevel = _roomGrain._petLevelProvider.GetMaxLevel(pet.Type);

        PetSnapshot updated = pet with { Experience = newExperience, Level = newLevel };
        _roomGrain._state.PetsById[pet.PetId] = updated;

        int xpForNext = _roomGrain._petLevelProvider.GetExperienceForNextLevel(
            updated.Type,
            updated.Level
        );
        int xpForNextSafe = xpForNext == int.MaxValue ? updated.Experience : xpForNext;

        await _roomGrain
            .SendComposerToRoomAsync(
                new PetExperienceMessageComposer
                {
                    PetId = updated.PetId,
                    Experience = updated.Experience,
                    ExperienceForNextLevel = xpForNextSafe,
                    Level = updated.Level,
                    MaxLevel = maxLevel,
                }
            )
            .ConfigureAwait(false);

        if (leveledUp)
        {
            await _roomGrain
                .SendComposerToRoomAsync(
                    new PetLevelUpdateMessageComposer
                    {
                        PetId = updated.PetId,
                        Level = updated.Level,
                    }
                )
                .ConfigureAwait(false);

            await _roomGrain
                .SendComposerToRoomAsync(
                    new UserUpdateMessageComposer
                    {
                        Avatars = [await ToAvatarSnapshotAsync(updated, ct).ConfigureAwait(false)],
                    }
                )
                .ConfigureAwait(false);

            await BroadcastPetVocalAsync(updated, "LEVEL_UP").ConfigureAwait(false);

            try
            {
                await _roomGrain
                    ._grainFactory.GetPlayerPresenceGrain(updated.OwnerId)
                    .SendComposerAsync(
                        new PetLevelNotificationEventMessageComposer
                        {
                            PetId = updated.PetId,
                            NewLevel = updated.Level,
                            PetName = updated.Name,
                        }
                    )
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _roomGrain._logger.LogError(
                    ex,
                    "Failed to send pet level-up notification for pet {PetId}",
                    updated.PetId
                );
            }
        }

        return updated;
    }
}
