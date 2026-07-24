using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>
/// Moves the selected furni one tile in a direction (Habbo's "move in a direction", client
/// MoveToDirection.ts). Int params are <c>[startDir, turn, blockOnCollide]</c>: <c>startDir</c> 0-7 is
/// a compass direction (the icon index maps straight to <see cref="Rotation"/>), <c>turn</c> is what to
/// do when the next tile is blocked (0 wait, 1 right 45°, 2 right 90°, 3 left 45°, 4 left 90°, 5 turn
/// back, 6 random), and <c>blockOnCollide</c> treats a tile with a user on it as blocked.
/// <para>
/// Each activation advances one tile — pairing it with a periodic trigger is what makes furni travel.
/// A furni keeps its current heading between activations (seeded from <c>startDir</c>); when blocked it
/// turns by the rule, remembers the new heading and moves that way if it can, otherwise it stays put
/// and retries next time. The heading is ephemeral runtime state and resets to <c>startDir</c> on room
/// reload.
/// </para>
/// </summary>
[RoomObjectLogic("wf_act_move_to_dir")]
public class WiredActionMoveToDirection(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    // Current heading per moved furni, kept across activations so the box "remembers" where each furni
    // is going after it has turned. Never persisted.
    private readonly Dictionary<int, Rotation> _headings = [];

    public override int WiredCode => (int)WiredActionType.MOVE_TO_DIRECTION;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(0, 7, 0), // start direction (compass)
            new WiredRangeParamRule(0, 6, 0), // turn-when-blocked rule
            new WiredBoolParamRule(false), // block when colliding with a user
        ];

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
        var startDir = (Rotation)(
            _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 0
        );
        int turn = _wiredData.IntParams.Count > 1 ? _wiredData.GetIntParam<int>(1) : 0;
        bool blockOnCollide = _wiredData.IntParams.Count > 2 && _wiredData.GetIntParam<bool>(2);

        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        foreach (int furniId in selection.SelectedFurniIds)
        {
            if (
                !_roomGrain._state.ItemsById.TryGetValue(furniId, out IRoomItem? item)
                || item is not IRoomFloorItem floorItem
            )
            {
                _headings.Remove(furniId);

                continue;
            }

            Rotation heading = _headings.TryGetValue(furniId, out Rotation stored)
                ? stored
                : startDir;

            // Try the current heading; if blocked, turn once and try that.
            if (await TryStepAsync(ctx, floorItem, heading, blockOnCollide))
            {
                _headings[furniId] = heading;

                continue;
            }

            Rotation turned = ApplyTurn(heading, turn);
            _headings[furniId] = turned;

            if (turned != heading)
            {
                await TryStepAsync(ctx, floorItem, turned, blockOnCollide);
            }
        }

        return true;
    }

    /// <summary>Moves the furni one tile in <paramref name="direction"/> if the target tile is free;
    /// returns whether it moved.</summary>
    private async Task<bool> TryStepAsync(
        IWiredExecutionContext ctx,
        IRoomFloorItem floorItem,
        Rotation direction,
        bool blockOnCollide
    )
    {
        int currentIdx = _roomGrain.MapModule.ToIdx(floorItem.X, floorItem.Y);

        if (!_roomGrain.MapModule.TryGetTileInFront(currentIdx, direction, out int nextIdx))
        {
            return false;
        }

        if (
            blockOnCollide && _roomGrain._state.TileFlags[nextIdx].Has(RoomTileFlags.AvatarOccupied)
        )
        {
            return false;
        }

        (int nextX, int nextY) = _roomGrain.MapModule.GetTileXY(nextIdx);

        if (
            !await _roomGrain.FurniModule.ValidateFloorItemPlacementAsync(
                ActionContext.Wired,
                floorItem.ObjectId,
                nextX,
                nextY,
                floorItem.Rotation
            )
        )
        {
            return false;
        }

        // null Z lets the move take the destination tile height; the furni slides, keeping its rotation.
        await ctx.ProcessFloorItemMovementAsync(floorItem, nextIdx, null, floorItem.Rotation);

        return true;
    }

    /// <summary>Applies the "when blocked" turn rule to a heading. Rotations step in 45° units.</summary>
    private static Rotation ApplyTurn(Rotation heading, int turn) =>
        turn switch
        {
            1 => heading.Rotate(1), // right 45°
            2 => heading.Rotate(2), // right 90°
            3 => heading.Rotate(-1), // left 45°
            4 => heading.Rotate(-2), // left 90°
            5 => heading.Rotate(4), // turn back
            6 => (Rotation)Random.Shared.Next(0, 8), // random
            _ => heading, // 0 = wait: keep heading, retry next time
        };
}
