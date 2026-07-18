using System.Collections.Generic;
using Orleans;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Rooms.Enums.Wired;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;
using Turbo.Primitives.Rooms.Wired;
using Turbo.Rooms.Wired.Rules;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>Passes when the number of players currently in the room is within the configured
/// [min, max] range (Habbo's "user count in room"). Int params: [0] = lower bound, [1] = upper bound.
/// The negative variant inherits this and flips <see cref="FurnitureWiredConditionLogic.IsNegative"/>.</summary>
[RoomObjectLogic("wf_cnd_user_count_in")]
public class WiredConditionRoomHabboCount(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.USER_COUNT_IN;

    // [0] = lower bound, [1] = upper bound. Rules must be declared or the client config update is rejected.
    public override List<IWiredParamRule> GetIntParamRules() =>
        [new WiredParamRule(0), new WiredParamRule(0)];

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        int userCount = _roomGrain._state.AvatarsByPlayerId.Count;
        int min = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 0;
        int max = _wiredData.IntParams.Count > 1 ? _wiredData.GetIntParam<int>(1) : int.MaxValue;

        bool result = userCount >= min && userCount <= max;

        return IsNegative() ? !result : result;
    }
}
