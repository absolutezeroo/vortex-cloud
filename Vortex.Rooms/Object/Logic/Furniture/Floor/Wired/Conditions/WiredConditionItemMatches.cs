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
/// "The selected furni still match their captured snapshot". Client StatesMatch.ts sends four
/// checkboxes — state, direction, position, altitude — as four ints.
/// <para>
/// The rules are declared so the box can be saved; the comparison itself needs the furni-snapshot
/// subsystem, which does not exist yet, so the condition currently never passes.
/// </para>
/// </summary>
[RoomObjectLogic("wf_cnd_match_snapshot_new")]
public class WiredConditionItemMatches(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.STATES_MATCH;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredBoolParamRule(true), // match state
            new WiredBoolParamRule(true), // match direction
            new WiredBoolParamRule(true), // match position
            new WiredBoolParamRule(true), // match altitude
        ];
}
