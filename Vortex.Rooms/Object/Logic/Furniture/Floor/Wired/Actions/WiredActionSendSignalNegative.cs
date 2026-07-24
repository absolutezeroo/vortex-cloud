using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>The negative furni variant of the send-signal action (client SendSignal.ts also returns
/// NEG_SEND_SIGNAL). It emits a signal exactly like <see cref="WiredActionSendSignal"/>; only the wire
/// code differs so both catalog furni round-trip correctly.</summary>
[RoomObjectLogic("wf_act_neg_send_signal")]
public class WiredActionSendSignalNegative(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : WiredActionSendSignal(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.NEG_SEND_SIGNAL;
}
