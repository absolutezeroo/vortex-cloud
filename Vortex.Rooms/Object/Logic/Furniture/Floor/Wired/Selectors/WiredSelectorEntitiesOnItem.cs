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

/// <summary>Selects the players standing on the input furni's tile (client UsersOnFurni.ts, no params).
/// Was a stub that selected nothing.</summary>
[RoomObjectLogic("wf_slc_users_onfurni")]
public class WiredSelectorEntitiesOnItem(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredSelectorLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredSelectorType.USERS_ON_FURNI;

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
        WiredSelectionSet output = new();

        foreach (int furniId in input.SelectedFurniIds)
        {
            if (
                _roomGrain._state.ItemsById.TryGetValue(furniId, out IRoomItem? item)
                && item is IRoomFloorItem floorItem
            )
            {
                AddPlayersOnTile(_roomGrain.MapModule.ToIdx(floorItem.X, floorItem.Y), output);
            }
        }

        return output;
    }
}
