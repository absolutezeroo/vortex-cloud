using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>Slides each selected furni a number of tiles in a cardinal direction (Habbo's "move furni
/// to", <c>wf_act_move_furni_to</c>). Client MoveFurniTo.ts: intParams = [direction (0/2/4/6), tiles].
/// A move that fails placement validation is skipped.</summary>
[RoomObjectLogic("wf_act_move_furni_to")]
public class WiredActionMoveFurniTo(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.MOVE_FURNI_TO;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [new WiredRangeParamRule(0, 7, 0), new WiredRangeParamRule(1, 100, 1)];

    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [
                WiredFurniSourceType.SelectedItems,
                WiredFurniSourceType.SelectorItems,
                WiredFurniSourceType.TriggeredItem,
            ],
        ];

    public override async Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
    {
        int direction = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 0;
        int tiles = _wiredData.IntParams.Count > 1 ? _wiredData.GetIntParam<int>(1) : 1;

        (int dx, int dy) = _roomGrain.MapModule.GetDirectionOffset((Rotation)direction);

        if ((dx == 0 && dy == 0) || tiles <= 0)
        {
            return true;
        }

        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        foreach (int furniId in selection.SelectedFurniIds)
        {
            if (
                !_roomGrain._state.ItemsById.TryGetValue(furniId, out IRoomItem? item)
                || item is not IRoomFloorItem floorItem
            )
            {
                continue;
            }

            int targetX = floorItem.X + (dx * tiles);
            int targetY = floorItem.Y + (dy * tiles);

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

        return true;
    }
}
