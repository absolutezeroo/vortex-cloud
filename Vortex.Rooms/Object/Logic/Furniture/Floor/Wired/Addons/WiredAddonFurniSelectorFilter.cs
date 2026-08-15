using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;

/// <summary>"WIRED Filter: Filter To X Furni" — the furni half of
/// <see cref="WiredAddonSelectorFilter"/>.</summary>
[RoomObjectLogic("wf_xtra_filter_furni")]
public class WiredAddonFurniSelectorFilter(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : WiredAddonSelectorFilter(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredAddonType.FURNI_SELECTOR_FILTER;

    protected override bool FiltersFurni => true;
}
