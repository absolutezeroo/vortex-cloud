using Orleans;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Players;
using Turbo.Primitives.Rooms.Enums.Wired;
using Turbo.Primitives.Rooms.Object;
using Turbo.Primitives.Rooms.Object.Avatars;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;
using Turbo.Primitives.Rooms.Wired;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>Passes when the triggering player's body faces one of the configured directions (Habbo's
/// "user direction"). Int param [0] is a bitmask of allowed rotations: direction <c>i</c> is allowed
/// when bit <c>i</c> is set (client stores it that way in the setup form's checkbox group).</summary>
[RoomObjectLogic("wf_cnd_actor_dir")]
public class WiredConditionEntityDirection(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.USER_DIRECTION;

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        PlayerId triggerer = ctx.Event.CausedBy.PlayerId;
        int mask = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 0;
        bool result = false;

        if (
            mask != 0
            && triggerer > 0
            && _roomGrain._state.AvatarsByPlayerId.TryGetValue(triggerer, out RoomObjectId objectId)
            && _roomGrain._state.AvatarsByObjectId.TryGetValue(objectId, out IRoomAvatar? avatar)
        )
        {
            result = (mask & (1 << (int)avatar.Rotation)) != 0;
        }

        return IsNegative() ? !result : result;
    }
}
