using System.Collections.Generic;
using Orleans;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Players;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Enums.Wired;
using Turbo.Primitives.Rooms.Object;
using Turbo.Primitives.Rooms.Object.Avatars;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;
using Turbo.Primitives.Rooms.Wired;
using Turbo.Rooms.Wired.Rules;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

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
            && _roomGrain._state.AvatarsByPlayerId.TryGetValue(triggerer, out RoomObjectId objectId)
            && _roomGrain._state.AvatarsByObjectId.TryGetValue(objectId, out IRoomAvatar? avatar)
            && avatar is IRoomPlayer player
        )
        {
            result = _wiredData.GetIntParam<int>(0) switch
            {
                0 => player.HasStatus(AvatarStatusType.Wave),
                6 => player.HasStatus(AvatarStatusType.Sit),
                7 => !player.HasStatus(AvatarStatusType.Sit, AvatarStatusType.Lay),
                8 => player.HasStatus(AvatarStatusType.Lay),
                10 => player.HasStatus(AvatarStatusType.Sign),
                11 => player.DanceType != AvatarDanceType.None,
                _ => false,
            };
        }

        return IsNegative() ? !result : result;
    }
}
