using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>
/// The negative furni of the send-signal action: it emits the same signal, but on the branch that
/// runs when the pile's trigger fired and its conditions did not hold.
/// </summary>
/// <remarks>
/// This class used to differ from <see cref="WiredActionSendSignal"/> by its wire code alone, which
/// made it fire on success — the exact opposite of what the box means, and invisible except in a
/// live room.
/// </remarks>
[RoomObjectLogic("wf_act_neg_send_signal")]
public class WiredActionSendSignalNegative(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : WiredActionSendSignal(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.NEG_SEND_SIGNAL;

    public override bool IsNegative() => true;
}
