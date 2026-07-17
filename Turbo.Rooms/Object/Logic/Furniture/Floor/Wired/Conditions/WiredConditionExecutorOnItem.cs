using Orleans;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Players;
using Turbo.Primitives.Rooms.Enums.Wired;
using Turbo.Primitives.Rooms.Object;
using Turbo.Primitives.Rooms.Object.Avatars;
using Turbo.Primitives.Rooms.Object.Furniture;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;
using Turbo.Primitives.Rooms.Wired;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>Passes when the player who triggered the stack is standing on one of the condition's own
/// configured furni (Habbo's "triggerer is on furni"). The negative variant inherits this and flips
/// <see cref="FurnitureWiredConditionLogic.IsNegative"/>.</summary>
[RoomObjectLogic("wf_cnd_trggrer_on_frn")]
public class WiredConditionExecutorOnItem(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.TRIGGERER_IS_ON_FURNI;

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        bool result = false;
        PlayerId triggerer = ctx.Event.CausedBy.PlayerId;

        if (
            triggerer > 0
            && _roomGrain._state.AvatarsByPlayerId.TryGetValue(triggerer, out RoomObjectId objectId)
            && _roomGrain._state.AvatarsByObjectId.TryGetValue(objectId, out IRoomAvatar? avatar)
            && avatar is IRoomPlayer player
        )
        {
            int playerIdx = _roomGrain.MapModule.ToIdx(player.X, player.Y);

            foreach (int furniId in GetStuffIds())
            {
                if (
                    _roomGrain._state.ItemsById.TryGetValue(furniId, out IRoomItem? item)
                    && item is IRoomFloorItem floor
                    && _roomGrain.MapModule.ToIdx(floor.X, floor.Y) == playerIdx
                )
                {
                    result = true;
                    break;
                }
            }
        }

        return IsNegative() ? !result : result;
    }
}
