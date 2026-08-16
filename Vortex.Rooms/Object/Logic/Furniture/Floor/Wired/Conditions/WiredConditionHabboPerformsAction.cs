using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>Passes when the triggering player is performing the configured action (Habbo's "user
/// performs action"). Int param [0] is the client's <c>WiredUserAction</c> code
/// (0=wave, 6=sit, 7=stand, 8=lay, 10=sign, 11=dance). Momentary expressions (blow/laugh/respect) and
/// idle sleep are not tracked as durable avatar state here, so they report false. The negative variant
/// inherits this and flips <see cref="FurnitureWiredConditionLogic.IsNegative"/>.</summary>
[RoomObjectLogic("wf_cnd_user_performs_action")]
public class WiredConditionHabboPerformsAction(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.PERFORMING_ACTION;

    // [0] = WiredUserAction code. Rules must be declared or the client config update is rejected.
    public override List<IWiredParamRule> GetIntParamRules() => [new WiredParamRule(0)];

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        PlayerId triggerer = ctx.Event.CausedBy.PlayerId;
        bool result = false;

        if (
            _wiredData.IntParams.Count > 0
            && triggerer > 0
            && _ctx.Lookup.TryFindAvatarByPlayer(triggerer, out IRoomAvatar? avatar)
            && avatar is IRoomPlayer player
        )
        {
            result = WiredUserActionMatcher.Matches(_wiredData.GetIntParam<int>(0), player);
        }

        return IsNegative() ? !result : result;
    }
}
