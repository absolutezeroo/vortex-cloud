using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;

/// <summary>"WIRED Filter: Furni with Highest/Lowest Variable" — the furni half of
/// <see cref="WiredAddonVariableSortFilter"/>.</summary>
[RoomObjectLogic("wf_xtra_filter_furni_by_var")]
public class WiredAddonFurniVariableFilter(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : WiredAddonVariableSortFilter(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredAddonType.FURNI_VARIABLE_FILTER;

    protected override bool FiltersFurni => true;
}
