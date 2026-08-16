using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>
/// "The user is carrying a given hand item". The client's dropdown sends the chosen hand-item code
/// as a single int, and code 0 is a real option on that list (<c>handitem0 = None</c>), so this is a
/// plain equality against what the avatar is holding rather than a "holding anything" test: leaving
/// the box on its first option asks for empty-handed users, which is what the form says.
/// </summary>
[RoomObjectLogic("wf_cnd_wears_handitem")]
[RoomObjectLogic("wf_cnd_has_handitem")]
public class WiredConditionHabboHasHanditem(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.ACTOR_HAS_HANDITEM;

    // The rule must be declared or the config update is rejected outright, which would make the box
    // unsaveable rather than merely inert.
    public override List<IWiredParamRule> GetIntParamRules() =>
        [new WiredRangeParamRule(0, 9999, 0)];

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        PlayerId triggerer = ctx.Event.CausedBy.PlayerId;
        int required = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 0;
        bool result = false;

        if (triggerer > 0 && _ctx.Lookup.TryFindAvatarByPlayer(triggerer, out IRoomAvatar? avatar))
        {
            // CarryItemId is cleared by the avatar tick when the item's time is up, so an expired
            // hand item reads as empty-handed here without this needing to know about the clock.
            result = avatar.CarryItemId == required;
        }

        return IsNegative() ? !result : result;
    }
}
