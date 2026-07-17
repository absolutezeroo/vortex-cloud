using System.Collections.Generic;
using System.Linq;
using Orleans;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Rooms.Enums.Wired;
using Turbo.Primitives.Rooms.Object.Furniture;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;
using Turbo.Primitives.Rooms.Wired;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>Passes when a furni that triggered the stack is of the same type (sprite) as one of the
/// condition's own configured furni (Habbo's "stuff type matches"). The negative variant inherits this
/// and flips <see cref="FurnitureWiredConditionLogic.IsNegative"/>.</summary>
[RoomObjectLogic("wf_cnd_stuff_is")]
public class WiredConditionItemPerType(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.STUFF_TYPE_MATCHES;

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        HashSet<int> configuredSprites = [];

        foreach (int furniId in GetStuffIds())
        {
            if (_roomGrain._state.ItemsById.TryGetValue(furniId, out IRoomItem? item))
            {
                configuredSprites.Add(item.Definition.SpriteId);
            }
        }

        bool result =
            configuredSprites.Count > 0
            && ctx.Selected.SelectedFurniIds.Any(triggeredId =>
                _roomGrain._state.ItemsById.TryGetValue(triggeredId, out IRoomItem? triggered)
                && configuredSprites.Contains(triggered.Definition.SpriteId)
            );

        return IsNegative() ? !result : result;
    }
}
