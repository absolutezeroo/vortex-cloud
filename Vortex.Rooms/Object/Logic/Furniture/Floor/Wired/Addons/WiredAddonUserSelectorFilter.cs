using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;

/// <summary>
/// The user half of <see cref="WiredAddonSelectorFilter"/> (add-on code 11). The client draws this
/// dialog from the same class as the furni one, but no furni in this hotel's furnidata carries the
/// classname today — the binding is kept so the box works the moment one does, rather than being
/// the only half of a pair that silently does nothing.
/// </summary>
[RoomObjectLogic("wf_xtra_filter_users")]
public class WiredAddonUserSelectorFilter(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : WiredAddonSelectorFilter(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredAddonType.USER_SELECTOR_FILTER;

    protected override bool FiltersFurni => false;
}
