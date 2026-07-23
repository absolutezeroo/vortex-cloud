using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>
/// The Timer Reset effect (<c>wf_act_reset_timers</c>): re-anchors every repeater in the room so each
/// fires on the next tick and restarts its interval from now. Room-wide, matching Habbo — the effect
/// targets the room, not just its own pile — so it takes no furni/player sources and no extra params
/// (only the standard per-effect delay handled by the base). Client: actiontypes/Reset.ts.
/// </summary>
[RoomObjectLogic("wf_act_reset_timers")]
public class WiredActionResetTimers(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.RESET;

    public override Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
    {
        _roomGrain.WiredSystem.ResetTimers();

        return Task.FromResult(true);
    }
}
