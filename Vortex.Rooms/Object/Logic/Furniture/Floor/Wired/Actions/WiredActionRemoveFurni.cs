using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>
/// Takes the selected furni out of the room and puts them in their owner's inventory.
/// </summary>
/// <remarks>
/// No form at all on the client's side (actiontypes/RemoveFurni): the box is its furni selection and
/// nothing else.
/// <para>
/// <b>Each furni goes to whoever owns it</b>, never to the player who set the trigger off. The
/// alternative is a room whose wiring pockets its visitors' furniture, which is a griefing tool
/// rather than a wired box. That is decided in the room rather than here — see
/// <see cref="Vortex.Primitives.Rooms.Object.IRoomFurniAccess.RemoveFurniFromWiredAsync"/> — so
/// nothing on this path can choose a different recipient.
/// </para>
/// <para>
/// The box itself is excluded, and so is any other wired box in the same pile. A stack that removed
/// the furniture it is standing on would delete itself mid-execution, and the rest of its actions
/// would run against an object that no longer exists.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_act_remove_furni_inventory")]
public class WiredActionRemoveFurni(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.REMOVE_FURNI;

    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [
                WiredFurniSourceType.SelectedItems,
                WiredFurniSourceType.SelectorItems,
                WiredFurniSourceType.SignalItems,
                WiredFurniSourceType.TriggeredItem,
            ],
        ];

    public override async Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
    {
        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        foreach (int furniId in selection.SelectedFurniIds)
        {
            if (
                !_ctx.Lookup.TryFindItem(furniId, out IRoomItem? item)
                || item.Logic is FurnitureWiredLogic
            )
            {
                continue;
            }

            await _ctx.Furni.RemoveFurniFromWiredAsync(item.ObjectId, ct);
        }

        return true;
    }
}
