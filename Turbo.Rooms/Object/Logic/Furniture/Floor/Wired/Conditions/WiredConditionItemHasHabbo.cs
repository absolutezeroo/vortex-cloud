using Orleans;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Rooms.Enums.Wired;
using Turbo.Primitives.Rooms.Object.Furniture;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;
using Turbo.Primitives.Rooms.Wired;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>Passes when at least one of the condition's own configured furni has an avatar standing on
/// its tile (Habbo's "furnis have Habbos"). The negative variant inherits this and flips
/// <see cref="FurnitureWiredConditionLogic.IsNegative"/>, so the result is negated automatically.</summary>
[RoomObjectLogic("wf_cnd_furnis_hv_avtrs")]
public class WiredConditionItemHasHabbo(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.FURNIS_HAVE_AVATARS;

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        bool result = false;

        foreach (int furniId in GetStuffIds())
        {
            if (
                !_roomGrain._state.ItemsById.TryGetValue(furniId, out IRoomItem? item)
                || item is not IRoomFloorItem floor
            )
            {
                continue;
            }

            int idx = _roomGrain.MapModule.ToIdx(floor.X, floor.Y);

            if (
                idx >= 0
                && idx < _roomGrain._state.TileAvatarStacks.Length
                && _roomGrain._state.TileAvatarStacks[idx].Count > 0
            )
            {
                result = true;
                break;
            }
        }

        return IsNegative() ? !result : result;
    }
}
