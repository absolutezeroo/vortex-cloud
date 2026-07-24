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
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>Shifts each selected furni by a fixed tile offset (Habbo's "relative furni move"). Int
/// params from the client setup form: [0] = signed horizontal distance, [1] = signed vertical distance
/// (sign encodes the direction). Rotation is unchanged; a move that fails placement validation is
/// skipped.</summary>
[RoomObjectLogic("wf_act_rel_mov")]
public class WiredActionRelativeMoveFurni(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.RELATIVE_FURNI_MOVE;

    // Client RelativeFurniMove.ts sends two signed offsets. They were read but never declared, and a
    // parameter count that does not match the client makes the update handler discard the entire box
    // configuration - so the box could not be saved at all.
    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(-20, 20, 0), // offset X
            new WiredRangeParamRule(-20, 20, 0), // offset Y
        ];

    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [
                WiredFurniSourceType.SelectedItems,
                WiredFurniSourceType.SelectorItems,
                WiredFurniSourceType.SignalItems,
                WiredFurniSourceType.TriggeredItem,
            ],
        ];

    public override async Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
    {
        int dx = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 0;
        int dy = _wiredData.IntParams.Count > 1 ? _wiredData.GetIntParam<int>(1) : 0;

        if (dx == 0 && dy == 0)
        {
            return true;
        }

        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        foreach (int furniId in selection.SelectedFurniIds)
        {
            try
            {
                if (
                    !_roomGrain._state.ItemsById.TryGetValue(furniId, out IRoomItem? item)
                    || item is not IRoomFloorItem floorItem
                )
                {
                    continue;
                }

                int targetX = floorItem.X + dx;
                int targetY = floorItem.Y + dy;

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
                        floorItem.Z,
                        floorItem.Rotation
                    );
                }
            }
            catch
            {
                continue;
            }
        }

        return true;
    }
}
