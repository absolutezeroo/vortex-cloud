using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Selectors;

/// <summary>Selects the players in the neighbourhood of each input furni/user (client
/// UsersInNeighborhood.ts). Reuses the furni neighbourhood selector's whole spiral/mask computation and
/// rule set, overriding only what is collected on each resolved tile. Was a stub that selected
/// nothing.</summary>
[RoomObjectLogic("wf_slc_users_neighborhood")]
public class WiredSelectorEntitiesInNeighborhood(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : WiredSelectorItemsInNeighborhood(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredSelectorType.USERS_IN_NEIGHBORHOOD;

    protected override void CollectFromTile(int tileId, WiredSelectionSet output) =>
        AddPlayersOnTile(tileId, output);
}
