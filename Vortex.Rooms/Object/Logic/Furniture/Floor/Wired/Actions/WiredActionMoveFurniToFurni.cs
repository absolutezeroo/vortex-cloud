using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>Moves the furni in the first selection slot onto the tile of the furni in the second slot
/// (Habbo's "move furni to furni", no int params). Slot 0 = StuffIds (the furni to move), slot 1 =
/// StuffIds2 (the destination furni). Invalid moves are skipped.</summary>
[RoomObjectLogic("wf_act_cnd_move_furni")]
public class WiredActionMoveFurniToFurni(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.MOVE_FURNI_TO_FURNI;

    // Two furni slots, matching the client's mv.0 ("the moving furni") and mv.1 ("the target furni").
    // Declaring them is what makes the client render the pickers at all — without it StuffIds and
    // StuffIds2 can never be filled and the action is inert however it is configured.
    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [
                WiredFurniSourceType.SelectedItems,
                WiredFurniSourceType.SelectorItems,
                WiredFurniSourceType.SignalItems,
            ],
            [WiredFurniSourceType.SelectedItems, WiredFurniSourceType.SelectorItems],
        ];

    public override async Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
    {
        List<int> destinationIds = GetStuffIds2();

        if (
            destinationIds.Count == 0
            || !TryResolveFurniTile(destinationIds[0], out int targetX, out int targetY)
        )
        {
            return true;
        }

        foreach (int furniId in GetStuffIds())
        {
            await TryMoveFurniAsync(ctx, furniId, targetX, targetY);
        }

        return true;
    }

    private bool TryResolveFurniTile(int furniId, out int x, out int y)
    {
        if (
            _roomGrain._state.ItemsById.TryGetValue(furniId, out IRoomItem? item)
            && item is IRoomFloorItem floorItem
        )
        {
            (x, y) = (floorItem.X, floorItem.Y);
            return true;
        }

        (x, y) = (0, 0);
        return false;
    }

    private async Task TryMoveFurniAsync(
        IWiredExecutionContext ctx,
        int furniId,
        int targetX,
        int targetY
    )
    {
        if (
            !_roomGrain._state.ItemsById.TryGetValue(furniId, out IRoomItem? item)
            || item is not IRoomFloorItem floorItem
        )
        {
            return;
        }

        if (
            await _roomGrain.FurniModule.ValidateFloorItemPlacementAsync(
                ActionContext.Wired,
                floorItem.ObjectId,
                targetX,
                targetY,
                floorItem.Rotation
            )
        )
        {
            await ctx.ProcessFloorItemMovementAsync(
                floorItem,
                _roomGrain.MapModule.ToIdx(targetX, targetY),
                null,
                floorItem.Rotation
            );
        }
    }
}
