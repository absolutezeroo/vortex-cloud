using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

[RoomObjectLogic("wf_cnd_not_user_performs_action")]
// The hotel's "user is NOT dancing" box — the negative twin of the alias on the positive condition.
[RoomObjectLogic("wf_cnd_not_habbo_is_dancing")]
public class WiredNegativeConditionHabboPerformsAction(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : WiredConditionHabboPerformsAction(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.NOT_PERFORMING_ACTION;

    public override bool IsNegative() => true;
}
