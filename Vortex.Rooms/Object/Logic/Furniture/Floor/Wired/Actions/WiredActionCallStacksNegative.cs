using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>
/// "WIRED Negative Effect: Execute Stacks" — the same call, on the other branch: it runs when the
/// pile's trigger fired but its conditions did not hold.
/// </summary>
[RoomObjectLogic("wf_act_neg_call_stacks")]
public class WiredActionCallStacksNegative(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : WiredActionCallStacks(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.NEG_CALL_ANOTHER_STACK;

    public override bool IsNegative() => true;
}
