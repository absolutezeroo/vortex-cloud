using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Selectors;

/// <summary>Selects the players inside a rectangular area (client UsersInArea.ts). Reuses the furni
/// area selector's [rootX, rootY, w, h] rule set and its tile computation (including the invert flag),
/// only collecting avatars on those tiles instead of furni. Was a stub that selected nothing.</summary>
[RoomObjectLogic("wf_slc_users_area")]
public class WiredSelectorEntitiesInArea(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : WiredSelectorItemsInArea(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredSelectorType.USERS_IN_AREA;

    public override Task<IWiredSelectionSet> SelectAsync(
        IWiredProcessingContext ctx,
        CancellationToken ct
    )
    {
        WiredSelectionSet output = new();

        foreach (int tileId in _tileIds)
        {
            AddPlayersOnTile(tileId, output);
        }

        return Task.FromResult((IWiredSelectionSet)output);
    }
}
