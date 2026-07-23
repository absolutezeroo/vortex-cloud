using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Action;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Snapshots.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired;

namespace Vortex.Rooms.Grains;

public sealed partial class RoomGrain
{
    public async Task<bool> PlaceFloorItemAsync(
        ActionContext ctx,
        FurnitureItemSnapshot item,
        int x,
        int y,
        Rotation rot,
        CancellationToken ct
    )
    {
        try
        {
            if (!await ActionModule.PlaceFloorItemAsync(ctx, item, x, y, rot, ct))
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to place floor item {ItemId} at ({X},{Y}) in room {RoomId}",
                item.ItemId,
                x,
                y,
                _state.RoomId
            );

            return false;
        }
    }

    public async Task<bool> MoveFloorItemByIdAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int x,
        int y,
        Rotation rot,
        CancellationToken ct
    )
    {
        try
        {
            if (!await ActionModule.MoveFloorItemByIdAsync(ctx, itemId, x, y, rot, ct))
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to move floor item {ItemId} to ({X},{Y}) in room {RoomId}",
                itemId,
                x,
                y,
                _state.RoomId
            );

            return false;
        }
    }

    public async Task<bool> ApplyWiredUpdateAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        UpdateWiredMessage update,
        CancellationToken ct
    )
    {
        try
        {
            if (!await ActionModule.ApplyWiredUpdateAsync(ctx, itemId, update, ct))
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to apply wired update to item {ItemId} in room {RoomId}",
                itemId,
                _state.RoomId
            );

            return false;
        }
    }

    public Task<RoomFloorItemSnapshot?> GetFloorItemSnapshotByIdAsync(
        RoomObjectId itemId,
        CancellationToken ct
    ) =>
        Task.FromResult(
            _state.ItemsById.TryGetValue(itemId, out IRoomItem? item)
            && item is IRoomFloorItem floorItem
                ? floorItem.GetSnapshot()
                : null
        );

    public Task<ImmutableArray<RoomFloorItemSnapshot>> GetAllFloorItemSnapshotsAsync(
        CancellationToken ct
    ) => FurniModule.GetAllFloorItemSnapshotsAsync(ct);

    public async Task<WiredDataSnapshot?> GetWiredDataSnapshotByFloorItemIdAsync(
        RoomObjectId itemId,
        CancellationToken ct
    )
    {
        if (
            !_state.ItemsById.TryGetValue(itemId, out IRoomItem? item)
            || item.Logic is not FurnitureWiredLogic wiredLogic
        )
        {
            return null;
        }

        // Make sure the box's persisted config is deserialized before we snapshot it — opening the
        // wired editor can happen before the wired tick has ever processed this furni's stack, and
        // BuildSnapshot dereferences the (otherwise null) wired data.
        await wiredLogic.LoadWiredAsync(ct);

        return wiredLogic.GetSnapshot();
    }

    public Task<WiredVariablesSnapshot> GetWiredVariablesSnapshotAsync(CancellationToken ct) =>
        WiredSystem.GetWiredVariablesSnapshotAsync(ct);

    public Task<
        List<(WiredVariableId id, WiredVariableValue value)>
    > GetAllVariablesForBindingAsync(WiredVariableBinding binding, CancellationToken ct) =>
        WiredSystem.GetAllVariablesForBindingAsync(binding, ct);

    public Task<(
        WiredVariableSnapshot Variable,
        List<(int ObjectId, int Value)> Holders
    )?> GetVariableHoldersByNameAsync(string variableName, CancellationToken ct) =>
        WiredSystem.GetVariableHoldersByNameAsync(variableName, ct);
}
