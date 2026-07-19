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
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Selectors;

[RoomObjectLogic("wf_slc_furni_bytype")]
public class WiredSelectorItemsByType(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredSelectorLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredSelectorType.FURNI_BY_TYPE;

    public override List<IWiredParamRule> GetIntParamRules() => [new WiredBoolParamRule(false)];

    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [
                WiredFurniSourceType.SelectedItems,
                WiredFurniSourceType.SignalItems,
                WiredFurniSourceType.TriggeredItem,
            ],
        ];

    public override async Task<IWiredSelectionSet> SelectAsync(
        IWiredProcessingContext ctx,
        CancellationToken ct
    )
    {
        IWiredSelectionSet input = await ctx.GetWiredSelectionSetAsync(this, ct);
        List<int> allowedDefinitionIds = new List<int>();
        WiredSelectionSet output = new WiredSelectionSet();

        foreach (int id in input.SelectedFurniIds)
        {
            try
            {
                if (!_roomGrain._state.ItemsById.TryGetValue(id, out IRoomItem? item))
                {
                    continue;
                }

                allowedDefinitionIds.Add(item.Definition.Id);
            }
            catch
            {
                continue;
            }
        }

        foreach (IRoomItem item in _roomGrain._state.ItemsById.Values)
        {
            if (allowedDefinitionIds.Contains(item.Definition.Id))
            {
                output.SelectedFurniIds.Add((int)item.ObjectId);
            }
        }

        return output;
    }
}
