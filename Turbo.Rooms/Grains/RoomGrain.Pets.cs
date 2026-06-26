using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Turbo.Primitives.Action;
using Turbo.Primitives.Pets.Snapshots;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Object;

namespace Turbo.Rooms.Grains;

public sealed partial class RoomGrain
{
    public async Task<PetSnapshot?> PlacePetAsync(
        ActionContext ctx,
        int petId,
        int x,
        int y,
        Rotation direction,
        CancellationToken ct
    )
    {
        try
        {
            return await PetSystem.PlacePetAsync(ctx, petId, x, y, direction, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to place pet {PetId} in room {RoomId} at ({X}, {Y})",
                petId,
                _state.RoomId,
                x,
                y
            );

            return null;
        }
    }

    public async Task<PetSnapshot?> MovePetAsync(
        ActionContext ctx,
        int petId,
        int x,
        int y,
        Rotation direction,
        CancellationToken ct
    )
    {
        try
        {
            return await PetSystem.MovePetAsync(ctx, petId, x, y, direction, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to move pet {PetId} in room {RoomId} to ({X}, {Y})",
                petId,
                _state.RoomId,
                x,
                y
            );

            return null;
        }
    }

    public async Task<PetSnapshot?> PickUpPetAsync(
        ActionContext ctx,
        int petId,
        CancellationToken ct
    )
    {
        try
        {
            return await PetSystem.PickUpPetAsync(ctx, petId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to pick up pet {PetId} in room {RoomId}",
                petId,
                _state.RoomId
            );

            return null;
        }
    }

    public async Task<PetFeedResult> FeedPetAsync(
        ActionContext ctx,
        int petId,
        RoomObjectId foodItemId,
        CancellationToken ct
    )
    {
        try
        {
            return await PetSystem.FeedPetAsync(ctx, petId, foodItemId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to feed pet {PetId} with food {FoodItemId} in room {RoomId}",
                petId,
                foodItemId,
                _state.RoomId
            );

            return PetFeedResult.Failed(foodItemId);
        }
    }

    public Task<PetSnapshot?> GetPlacedPetSnapshotAsync(int petId, CancellationToken ct) =>
        PetSystem.GetPlacedPetSnapshotAsync(petId, ct);

    public Task<ImmutableArray<PetSnapshot>> GetPlacedPetSnapshotsAsync(CancellationToken ct) =>
        PetSystem.GetPlacedPetSnapshotsAsync(ct);

    public async Task<PetSnapshot?> RespectPetAsync(
        ActionContext ctx,
        int petId,
        CancellationToken ct
    )
    {
        try
        {
            return await PetSystem.RespectPetAsync(ctx, petId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to respect pet {PetId} in room {RoomId}",
                petId,
                _state.RoomId
            );

            return null;
        }
    }

    public async Task<PetSnapshot?> GrantPetCommandXpAsync(
        ActionContext ctx,
        int petId,
        CancellationToken ct
    )
    {
        try
        {
            return await PetSystem.GrantPetCommandXpAsync(ctx, petId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to grant command XP to pet {PetId} in room {RoomId}",
                petId,
                _state.RoomId
            );

            return null;
        }
    }

    public async Task<PetSnapshot?> GiveSupplementToPetAsync(
        ActionContext ctx,
        int petId,
        CancellationToken ct
    )
    {
        try
        {
            return await PetSystem.GiveSupplementToPetAsync(ctx, petId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to give supplement to pet {PetId} in room {RoomId}",
                petId,
                _state.RoomId
            );

            return null;
        }
    }

    public async Task TogglePetBreedingPermissionAsync(
        ActionContext ctx,
        int petId,
        CancellationToken ct
    )
    {
        try
        {
            await PetSystem.TogglePetBreedingPermissionAsync(ctx, petId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to toggle breeding permission for pet {PetId} in room {RoomId}",
                petId,
                _state.RoomId
            );
        }
    }

    public async Task<bool> BreedPetsAsync(
        ActionContext ctx,
        int petOneId,
        int petTwoId,
        CancellationToken ct
    )
    {
        try
        {
            return await PetSystem.BreedPetsAsync(ctx, petOneId, petTwoId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to breed pets {PetOneId}+{PetTwoId} in room {RoomId}",
                petOneId,
                petTwoId,
                _state.RoomId
            );

            return false;
        }
    }

    public async Task<bool> ConfirmPetBreedingAsync(
        ActionContext ctx,
        int petId,
        CancellationToken ct
    )
    {
        try
        {
            return await PetSystem.ConfirmPetBreedingAsync(ctx, petId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to confirm pet breeding for pet {PetId} in room {RoomId}",
                petId,
                _state.RoomId
            );

            return false;
        }
    }

    public async Task CancelPetBreedingAsync(ActionContext ctx, int petId, CancellationToken ct)
    {
        try
        {
            await PetSystem.CancelPetBreedingAsync(ctx, petId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to cancel pet breeding for pet {PetId} in room {RoomId}",
                petId,
                _state.RoomId
            );
        }
    }

    public async Task<PetSnapshot?> IssueCommandAsync(
        ActionContext ctx,
        int petId,
        int commandId,
        CancellationToken ct
    )
    {
        try
        {
            return await PetSystem.IssueCommandAsync(ctx, petId, commandId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to issue command {CommandId} to pet {PetId} in room {RoomId}",
                commandId,
                petId,
                _state.RoomId
            );

            return null;
        }
    }
}
