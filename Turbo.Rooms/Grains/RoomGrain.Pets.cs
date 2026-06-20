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
}
