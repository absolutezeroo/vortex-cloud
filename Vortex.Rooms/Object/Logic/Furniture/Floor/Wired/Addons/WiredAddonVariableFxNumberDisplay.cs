using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;

/// <summary>The variable's value drawn as a number rather than a bar.</summary>
/// <remarks>
/// One of the six Variable FX displays. Everything but the code and the category lives in
/// <see cref="FurnitureWiredVariableFxAddonLogic"/>, exactly as the client splits them.
/// </remarks>
[RoomObjectLogic("wf_xtra_fx_number_display")]
public class WiredAddonVariableFxNumberDisplay(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredVariableFxAddonLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredAddonType.VARIABLE_FX_NUMBER_DISPLAY;

    protected override int CategoryId => 5;

    /// <summary>Twenty-two, not twenty-one: the icon has a side to sit on.</summary>
    /// <remarks>
    /// This is the one display that also uses the string param -- it holds the icon's name -- and
    /// the twenty-second int says which side of the number the icon goes. A box whose rule list is
    /// one short of what the form sends rejects the whole save, silently from the user's side.
    /// </remarks>
    public override List<IWiredParamRule> GetIntParamRules()
    {
        List<IWiredParamRule> rules = base.GetIntParamRules();

        rules.Add(new WiredRangeParamRule(0, int.MaxValue, 0)); // 21 icon alignment id

        return rules;
    }
}
