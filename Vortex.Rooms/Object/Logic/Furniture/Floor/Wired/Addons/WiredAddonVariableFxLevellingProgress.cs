using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;

/// <summary>A levelling-progress bar drawn from the variable it is stacked on.</summary>
/// <remarks>
/// One of the six Variable FX displays. Everything but the code and the category lives in
/// <see cref="FurnitureWiredVariableFxAddonLogic"/>, exactly as the client splits them.
/// </remarks>
[RoomObjectLogic("wf_xtra_fx_levelling_progress")]
public class WiredAddonVariableFxLevellingProgress(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredVariableFxAddonLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredAddonType.VARIABLE_FX_LEVELLING_PROGRESS;

    protected override int CategoryId => 2;

    /// <summary>Twenty-two, not twenty-one: this display draws a second one inside itself.</summary>
    /// <remarks>
    /// The badge has a progress bar under it, and that bar has a renderer of its own. The client
    /// writes its id as a twenty-second int param, and a box whose rule list is one short of what
    /// the form sends rejects the whole save -- silently, from the user's side, because
    /// <c>TryNormalizeIntParams</c> refuses a count it has no rule for and the update never lands.
    /// </remarks>
    public override List<IWiredParamRule> GetIntParamRules()
    {
        List<IWiredParamRule> rules = base.GetIntParamRules();

        rules.Add(new WiredRangeParamRule(0, int.MaxValue, 0)); // 21 sub-renderer id

        return rules;
    }
}
