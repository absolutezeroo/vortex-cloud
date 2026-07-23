using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;

/// <summary>Habbo's OR-eval addon: its mere presence on the pile switches condition evaluation from
/// "all conditions must pass" to "at least one condition must pass". It has no configuration.</summary>
[RoomObjectLogic("wf_xtra_or_eval")]
public class WiredAddonConditionsEval(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredAddonLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredAddonType.CONDITION_EVALUATION;

    public override Task<bool> MutatePolicyAsync(IWiredProcessingContext ctx, CancellationToken ct)
    {
        ctx.Policy.ConditionMode = WiredConditionModeType.Any;

        return Task.FromResult(true);
    }
}
