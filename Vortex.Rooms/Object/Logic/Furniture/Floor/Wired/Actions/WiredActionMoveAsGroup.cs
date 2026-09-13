using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>
/// Moves several furni at once while keeping their arrangement — a build travels instead of being
/// scattered across one tile.
/// </summary>
/// <remarks>
/// Int params from the client's form (actiontypes/MoveAsGroup): [0] whether the target location is a
/// user rather than a furni, [1] and [2] the x and y offsets, each -64..64. The furni to move are
/// slot 0; the target location is a merged source the client addresses as furni slot 1 and user slot
/// 0 (<c>mergedSelections()</c> returns <c>[1, 0]</c>), so three source lists have to be declared or
/// its picker reads past the end of the one it was handed.
/// <para>
/// <b>The anchor is the group's own corner</b> — the lowest x and y any member occupies. The client
/// says which tile to travel to and by how much to offset it, and nothing in it says which member
/// lands there; the corner is the choice that keeps the arrangement intact whichever member happens
/// to be listed first, which is the property the box is named for.
/// </para>
/// <para>
/// <b>All or nothing.</b> Every destination is validated before anything moves, and one refusal
/// cancels the whole move. Moving the members that fit and leaving the rest is how a group move
/// turns a build into rubble, and there is no undo for it in a room.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_act_move_as_group")]
public class WiredActionMoveAsGroup(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.MOVE_AS_GROUP;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(0, 1, 1), // the target is a user rather than a furni
            new WiredRangeParamRule(-64, 64, 0), // x offset
            new WiredRangeParamRule(-64, 64, 0), // y offset
        ];

    /// <summary>Slot 0 is the group; slot 1 is the target location's furni half.</summary>
    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [.. FurniSources],
            [.. FurniSources],
        ];

    /// <summary>Slot 0 is the target location's user half.</summary>
    public override List<WiredPlayerSourceType[]> GetAllowedPlayerSources() =>
        [
            [.. PlayerSources],
        ];

    private static readonly WiredFurniSourceType[] FurniSources =
    [
        WiredFurniSourceType.SelectedItems,
        WiredFurniSourceType.SelectorItems,
        WiredFurniSourceType.SignalItems,
        WiredFurniSourceType.TriggeredItem,
    ];

    private static readonly WiredPlayerSourceType[] PlayerSources =
    [
        WiredPlayerSourceType.TriggeredUser,
        WiredPlayerSourceType.SelectorUsers,
        WiredPlayerSourceType.SignalUsers,
    ];

    public override async Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
    {
        if (_wiredData.IntParams.Count < 3)
        {
            return true;
        }

        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        // The room resolves every slot of a box into one selection, so the target furni would
        // otherwise travel along with the group it is the destination for. Its configured id is the
        // one thing that still distinguishes the two.
        HashSet<int> targetIds = [.. GetStuffIds2()];

        List<IRoomFloorItem> group =
        [
            .. selection
                .SelectedFurniIds.Where(id => !targetIds.Contains(id))
                .Select(id =>
                    _ctx.Lookup.TryFindItem(id, out IRoomItem? item) ? item as IRoomFloorItem : null
                )
                .OfType<IRoomFloorItem>(),
        ];

        if (group.Count == 0 || !TryResolveTargetTile(selection, targetIds, out int tx, out int ty))
        {
            return true;
        }

        int dx = tx + _wiredData.GetIntParam<int>(1) - group.Min(item => item.X);
        int dy = ty + _wiredData.GetIntParam<int>(2) - group.Min(item => item.Y);

        if (dx == 0 && dy == 0)
        {
            return true;
        }

        foreach (IRoomFloorItem item in group)
        {
            if (
                !await _ctx.Furni.ValidateFloorItemPlacementAsync(
                    ActionContext.Wired,
                    item.ObjectId,
                    item.X + dx,
                    item.Y + dy,
                    item.Rotation
                )
            )
            {
                return true;
            }
        }

        foreach (IRoomFloorItem item in group)
        {
            await ctx.ProcessFloorItemMovementAsync(
                item,
                _ctx.Map.ToIdx(item.X + dx, item.Y + dy),
                null,
                item.Rotation
            );
        }

        return true;
    }

    /// <summary>Where the group's corner is headed: a resolved user's tile, or the configured target
    /// furni's.</summary>
    private bool TryResolveTargetTile(
        IWiredSelectionSet selection,
        HashSet<int> targetIds,
        out int x,
        out int y
    )
    {
        if (_wiredData.GetIntParam<int>(0) != 0)
        {
            foreach (int playerId in selection.SelectedPlayerIds)
            {
                if (_ctx.Lookup.TryFindAvatarByPlayer(playerId, out IRoomAvatar? avatar))
                {
                    (x, y) = (avatar.X, avatar.Y);

                    return true;
                }
            }

            (x, y) = (0, 0);

            return false;
        }

        foreach (int furniId in targetIds)
        {
            if (
                _ctx.Lookup.TryFindItem(furniId, out IRoomItem? item)
                && item is IRoomFloorItem floorItem
            )
            {
                (x, y) = (floorItem.X, floorItem.Y);

                return true;
            }
        }

        (x, y) = (0, 0);

        return false;
    }
}
