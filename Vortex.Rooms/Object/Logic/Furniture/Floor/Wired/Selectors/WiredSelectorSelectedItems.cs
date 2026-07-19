using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Selectors;

[RoomObjectLogic("wf_slc_furni_picks")]
public class WiredSelectorSelectedItems(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredSelectorLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredSelectorType.SELECTED_FURNIS;

    public override bool SupportsAdvancedMode() => false;

    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [WiredFurniSourceType.SelectedItems],
        ];

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
                if (!_roomGrain._state.ItemsById.TryGetValue(id, out IRoomItem? item))
                {
                    continue;
                }

                output.SelectedFurniIds.Add((int)item.ObjectId);
            }
            catch
            {
                continue;
            }
        }

        return output;
    }
}
