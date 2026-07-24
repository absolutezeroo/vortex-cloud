using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>Passes when at least one of the condition's own configured furni has another furni stacked
/// above it on the same tile (Habbo's "furni has furni on it"). The negative variant inherits this and
/// flips <see cref="FurnitureWiredConditionLogic.IsNegative"/>.</summary>
[RoomObjectLogic("wf_cnd_has_furni_on")]
public class WiredConditionItemHasItems(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.HAS_STACKED_FURNIS;

    // Client radio (loc requireall.*): 0 "if one of the selected furni", 1 "if all of them". It was
    // neither declared - so the box could not be saved - nor read, so it always behaved as "one".
    public override List<IWiredParamRule> GetIntParamRules() => [new WiredBoolParamRule(false)];

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        bool requireAll = _wiredData.IntParams.Count > 0 && _wiredData.GetIntParam<bool>(0);
        int considered = 0;
        int stacked = 0;

        foreach (int furniId in GetStuffIds())
        {
            if (
                !_roomGrain._state.ItemsById.TryGetValue(furniId, out IRoomItem? item)
                || item is not IRoomFloorItem floor
            )
            {
                continue;
            }

            int idx = _roomGrain.MapModule.ToIdx(floor.X, floor.Y);

            if (idx < 0 || idx >= _roomGrain._state.TileFloorStacks.Length)
            {
                continue;
            }

            considered++;
            bool hasStacked = false;

            foreach (RoomObjectId otherId in _roomGrain._state.TileFloorStacks[idx])
            {
                if (
                    otherId != floor.ObjectId
                    && _roomGrain._state.ItemsById.TryGetValue(otherId, out IRoomItem? other)
                    && other is IRoomFloorItem otherFloor
                    && otherFloor.Z > floor.Z
                )
                {
                    hasStacked = true;
                    break;
                }
            }

            if (hasStacked)
            {
                stacked++;
            }
        }

        bool result = considered > 0 && (requireAll ? stacked == considered : stacked > 0);

        return IsNegative() ? !result : result;
    }
}
