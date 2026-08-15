using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>The negative furni of <see cref="WiredConditionHasVariable"/> — same form and same
/// reading, flipped. The client declares both codes on one class (code / negativeCode), so the two
/// furni share a configuration exactly.</summary>
[RoomObjectLogic("wf_cnd_neg_has_var")]
public class WiredNegativeConditionHasVariable(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : WiredConditionHasVariable(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.NOT_HAS_VARIABLE;

    public override bool IsNegative() => true;
}
