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

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>Sets each selected furni to a random valid state (Habbo's "toggle furni state randomly").
/// Mirrors <see cref="WiredActionToggleItemState"/> but picks a random state instead of cycling.</summary>
[RoomObjectLogic("wf_act_toggle_to_rnd")]
public class WiredActionToggleRandomState(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.TOGGLE_TO_RANDOM_STATE;

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
        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        foreach (int furniId in selection.SelectedFurniIds)
        {
            try
            {
                if (!_roomGrain._state.ItemsById.TryGetValue(furniId, out IRoomItem? item))
                {
                    continue;
                }

                await ctx.ProcessItemStateUpdateAsync(item, item.Logic.GetRandomToggleableState());
            }
            catch
            {
                continue;
            }
        }

        return true;
    }
}
