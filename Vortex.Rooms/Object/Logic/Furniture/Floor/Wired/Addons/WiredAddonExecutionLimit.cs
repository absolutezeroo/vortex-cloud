using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;

/// <summary>
/// "WIRED Add-on: Execution Limit" — caps how many times the pile may run inside a rolling window.
/// </summary>
/// <remarks>
/// Two sliders: the amount (1-100) and the window (1-20). The window is sent in pulses, not in
/// seconds — its label reads "Time window: N seconds" while the slider stores Habbo's half-second
/// pulse, so 20 is ten seconds and reading it as seconds gives a window twenty times too long.
/// <para>
/// It was registered and inert, which for a limit means no limit at all.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_xtra_execution_limit")]
public class WiredAddonExecutionLimit(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredAddonLogic(grainFactory, stuffDataFactory, ctx)
{
    private const int MaxExecutions = 100;

    private const int MaxWindowPulses = 20;

    private const int MsPerPulse = 500;

    public override int WiredCode => (int)WiredAddonType.EXECUTION_LIMIT;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(1, MaxExecutions, 1),
            new WiredRangeParamRule(1, MaxWindowPulses, 1),
        ];

    public override Task<bool> MutatePolicyAsync(IWiredProcessingContext ctx, CancellationToken ct)
    {
        if (_wiredData.IntParams.Count < 2)
        {
            return Task.FromResult(true);
        }

        ctx.Policy.ExecutionLimit = _wiredData.GetIntParam<int>(0);
        ctx.Policy.ExecutionWindowMs = _wiredData.GetIntParam<int>(1) * MsPerPulse;

        return Task.FromResult(true);
    }
}
