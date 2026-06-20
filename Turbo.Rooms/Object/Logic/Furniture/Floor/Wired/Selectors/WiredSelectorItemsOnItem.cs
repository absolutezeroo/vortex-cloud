using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Rooms.Enums.Wired;
using Turbo.Primitives.Rooms.Object.Furniture;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;
using Turbo.Primitives.Rooms.Wired;
using Turbo.Rooms.Wired;
using Turbo.Rooms.Wired.Rules;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor.Wired.Selectors;

[RoomObjectLogic("wf_slc_furni_onfurni")]
public class WiredSelectorItemsOnItem(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredSelectorLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredSelectorType.FURNI_ON_FURNI;

    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [
                WiredFurniSourceType.SelectedItems,
                WiredFurniSourceType.SignalItems,
                WiredFurniSourceType.TriggeredItem,
            ],
        ];

    public override List<IWiredParamRule> GetIntParamRules() =>
        [new WiredEnumParamRule<WiredFurniSelectionType>(WiredFurniSelectionType.FurniAboveFurni)];

    public override async Task<IWiredSelectionSet> SelectAsync(
        IWiredProcessingContext ctx,
        CancellationToken ct
    )
    {
        IWiredSelectionSet input = await ctx.GetWiredSelectionSetAsync(this, ct);
        WiredSelectionSet output = new WiredSelectionSet();

        foreach (int id in input.SelectedFurniIds)
        {
            try
            {
                if (
                    !_roomGrain._state.ItemsById.TryGetValue(id, out IRoomItem? item)
                    || item is not IRoomFloorItem floorItem
                )
                {
                    continue;
                }

                int tileIdx = _roomGrain.MapModule.ToIdx(floorItem.X, floorItem.Y);
                IEnumerable<IRoomItem> floorStack = _roomGrain
                    ._state.TileFloorStacks[tileIdx]
                    .Select(x => _roomGrain._state.ItemsById[(int)x]);

                switch (_wiredData.GetIntParam<WiredFurniSelectionType>(0))
                {
                    case WiredFurniSelectionType.FurniAboveFurni:
                    {
                        IEnumerable<IRoomItem> aboveItems = floorStack.Where(i => i.Z > floorItem.Z);

                        foreach (IRoomItem aboveItem in aboveItems)
                            output.SelectedFurniIds.Add((int)aboveItem.ObjectId);

                        break;
                    }
                    case WiredFurniSelectionType.FurniUnderFurni:
                    {
                        IEnumerable<IRoomItem> belowItems = floorStack.Where(i => i.Z < floorItem.Z);

                        foreach (IRoomItem belowItem in belowItems)
                            output.SelectedFurniIds.Add((int)belowItem.ObjectId);

                        break;
                    }
                    case WiredFurniSelectionType.FurniHeightMatches:
                    {
                        IEnumerable<IRoomItem> sameHeightItems = floorStack.Where(i =>
                            i.Z == floorItem.Z && i.ObjectId != floorItem.ObjectId
                        );

                        foreach (IRoomItem sameHeightItem in sameHeightItems)
                            output.SelectedFurniIds.Add((int)sameHeightItem.ObjectId);

                        break;
                    }
                    case WiredFurniSelectionType.AllFurniOnTile:
                    {
                        foreach (IRoomItem stackItem in floorStack)
                            output.SelectedFurniIds.Add((int)stackItem.ObjectId);

                        break;
                    }
                }
            }
            catch
            {
                continue;
            }
        }

        return output;
    }
}
