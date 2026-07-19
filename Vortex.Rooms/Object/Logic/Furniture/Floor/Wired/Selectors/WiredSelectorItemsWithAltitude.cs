using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Selectors;

[RoomObjectLogic("wf_slc_furni_altitude")]
public class WiredSelectorItemsWithAltitude(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredSelectorLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredSelectorType.FURNI_WITH_ALTITUDE;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(0, 8000, 0),
            new WiredEnumParamRule<WiredComparisonType>(
                WiredComparisonType.LessThan,
                WiredComparisonType.LessThan,
                WiredComparisonType.Equals,
                WiredComparisonType.GreaterThan
            ),
        ];

    public override Task<IWiredSelectionSet> SelectAsync(
        IWiredProcessingContext ctx,
        CancellationToken ct
    )
    {
        Altitude altitude = Altitude.FromInt(_wiredData.GetIntParam<int>(0));
        WiredSelectionSet output = new WiredSelectionSet();

        foreach (IRoomItem item in _roomGrain._state.ItemsById.Values)
        {
            if (item is not IRoomFloorItem floorItem)
            {
                continue;
            }

            switch (_wiredData.GetIntParam<WiredComparisonType>(1))
            {
                case WiredComparisonType.LessThan:
                    if (floorItem.Z < altitude)
                    {
                        output.SelectedFurniIds.Add(item.ObjectId.Value);
                    }

                    continue;
                case WiredComparisonType.GreaterThan:
                    if (floorItem.Z > altitude)
                    {
                        output.SelectedFurniIds.Add(item.ObjectId.Value);
                    }

                    continue;
                case WiredComparisonType.Equals:
                    if (floorItem.Z == altitude)
                    {
                        output.SelectedFurniIds.Add(item.ObjectId.Value);
                    }

                    continue;
            }
        }

        return Task.FromResult((IWiredSelectionSet)output);
    }
}
