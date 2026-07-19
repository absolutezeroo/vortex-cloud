using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>Sets the vertical altitude of each selected floor furni (Habbo's "set furni altitude").
/// Int params from the client setup form (actiontypes/class_3827): [0] = altitude in hundredths of a
/// tile (slider range 0-8000, i.e. 0.00-80.00), [1] = operator. The three furnidata variants
/// (wf_act_set_altitude / wf_act_raise_furni / wf_act_lower_furni) all drive this one action code and
/// emit the operator, so the operator selects how the value is applied: 0 = set to the absolute
/// altitude, 1 = raise by it, 2 = lower by it (grounded in the furni key names — verify the exact
/// operator ids against a live setup if a variant misbehaves). The result is clamped to the same
/// 0-8000 range and applied in place (same tile / rotation) since only Z changes.</summary>
[RoomObjectLogic("wf_act_set_altitude")]
public class WiredActionSetFurniAltitude(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    private const int MinAltitude = 0;
    private const int MaxAltitude = 8000;

    public override int WiredCode => (int)WiredActionType.SET_FURNI_ALTITUDE;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(MinAltitude, MaxAltitude, 0), // altitude (hundredths)
            new WiredRangeParamRule(0, 2, 0), // operator: 0 = set, 1 = raise, 2 = lower
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
        int value = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 0;
        int op = _wiredData.IntParams.Count > 1 ? _wiredData.GetIntParam<int>(1) : 0;

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

                int current = floorItem.Z.ToInt();

                int target = op switch
                {
                    1 => current + value,
                    2 => current - value,
                    _ => value,
                };

                target = Math.Clamp(target, MinAltitude, MaxAltitude);

                if (target == current)
                {
                    continue;
                }

                await ctx.ProcessFloorItemMovementAsync(
                    floorItem,
                    _roomGrain.MapModule.ToIdx(floorItem.X, floorItem.Y),
                    Altitude.FromInt(target),
                    floorItem.Rotation
                );
            }
            catch
            {
                continue;
            }
        }

        return true;
    }
}
