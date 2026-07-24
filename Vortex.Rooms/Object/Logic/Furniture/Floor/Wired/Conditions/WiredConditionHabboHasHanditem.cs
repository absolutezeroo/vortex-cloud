using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>
/// "The user is carrying a given hand item". Client ActorHasHanditem.ts sends the chosen hand-item
/// code as a single int (0 when the dropdown is left empty).
/// <para>
/// The rule must be declared even though the check itself is still missing: the config update is
/// rejected outright when the declared parameter count does not match what the client sends, which
/// would make the box unsaveable rather than merely inert.
/// </para>
/// </summary>
[RoomObjectLogic("wf_cnd_wears_handitem")]
public class WiredConditionHabboHasHanditem(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.ACTOR_HAS_HANDITEM;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [new WiredRangeParamRule(0, 9999, 0)];
}
