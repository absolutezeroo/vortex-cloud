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
/// "WIRED Add-on: Random" — the pile runs a few of its effects at random instead of all of them.
/// </summary>
/// <remarks>
/// Two sliders: "Pick N effects" (1-100) and "Avoid effects from last M executions" (0-100). Note
/// the wire order is the reverse of the screen order — the form draws the pick slider first and
/// sends the skip one first (<c>[skips, picks]</c>), so reading them in the order they appear gives
/// a box that picks as many effects as it was told to avoid.
/// <para>
/// It was registered and inert: the pile ran every effect regardless.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_xtra_random")]
public class WiredAddonRandomActions(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredAddonLogic(grainFactory, stuffDataFactory, ctx)
{
    private const int MaxSkips = 100;

    private const int MaxPicks = 100;

    public override int WiredCode => (int)WiredAddonType.RANDOM_ACTION;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [new WiredRangeParamRule(0, MaxSkips, 0), new WiredRangeParamRule(1, MaxPicks, 1)];

    public override Task<bool> MutatePolicyAsync(IWiredProcessingContext ctx, CancellationToken ct)
    {
        if (_wiredData.IntParams.Count < 2)
        {
            return Task.FromResult(true);
        }

        ctx.Policy.EffectMode = WiredEffectModeType.Random;
        ctx.Policy.EffectAvoidRecentExecutions = _wiredData.GetIntParam<int>(0);
        ctx.Policy.EffectPickCount = _wiredData.GetIntParam<int>(1);

        return Task.FromResult(true);
    }
}
