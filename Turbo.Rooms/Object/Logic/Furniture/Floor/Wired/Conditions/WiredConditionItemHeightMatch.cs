using Orleans;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Rooms.Enums.Wired;
using Turbo.Primitives.Rooms.Object.Furniture;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;
using Turbo.Primitives.Rooms.Wired;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>Passes when one of the condition's own configured furni sits at the configured altitude,
/// compared with the chosen operator (Habbo's "furni has altitude"). Int params (from the client's
/// setup form): [0] = target altitude in hundredths of a tile (0-8000), [1] = comparison operator
/// (0 = equal, 1 = less than, 2 = greater than). This box has no negative variant; the operator
/// already covers the inverse cases.</summary>
[RoomObjectLogic("wf_cnd_has_altitude")]
public class WiredConditionItemHeightMatch(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.FURNI_HAS_ALTITUDE;

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        if (_wiredData.IntParams.Count < 2)
        {
            return false;
        }

        int target = _wiredData.GetIntParam<int>(0);
        int op = _wiredData.GetIntParam<int>(1);
        bool result = false;

        foreach (int furniId in GetStuffIds())
        {
            if (
                !_roomGrain._state.ItemsById.TryGetValue(furniId, out IRoomItem? item)
                || item is not IRoomFloorItem floor
            )
            {
                continue;
            }

            int altitude = floor.Z.ToInt();
            bool match = op switch
            {
                0 => altitude == target,
                1 => altitude < target,
                2 => altitude > target,
                _ => false,
            };

            if (match)
            {
                result = true;
                break;
            }
        }

        return result;
    }
}
