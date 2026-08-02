using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>Moves each selected furni one tile directly away from the nearest player (Habbo's "flee").
/// The mirror image of <see cref="WiredActionChaseHabbo"/>: same neighbour search, opposite step.</summary>
[RoomObjectLogic("wf_act_flee")]
public class WiredActionFleeHabbo(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.FLEE;

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
            try
            {
                if (
                    !_ctx.Lookup.TryFindItem(furniId, out IRoomItem? item)
                    || item is not IRoomFloorItem floorItem
                )
                {
                    continue;
                }

                int floorIdx = _ctx.Map.ToIdx(floorItem.X, floorItem.Y);
                int nearestIdx = -1;
                int bestDistance = int.MaxValue;

                foreach (IRoomAvatar avatar in _ctx.Lookup.Avatars)
                {
                    if (avatar is not IRoomPlayer player)
                    {
                        continue;
                    }

                    int playerIdx = _ctx.Map.ToIdx(player.X, player.Y);
                    int distance = _ctx.Map.GetDistanceBetween(floorIdx, playerIdx);

                    if (distance > 3)
                    {
                        continue;
                    }

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        nearestIdx = playerIdx;
                    }
                }

                int targetIdx;

                if (nearestIdx > -1)
                {
                    targetIdx = GetAwayTileIdx(floorIdx, nearestIdx);
                }
                else
                {
                    Rotation direction = RotationExtensions.CARDINAL[Random.Shared.Next(0, 4)];

                    if (!_ctx.Map.TryGetTileInFront(floorIdx, direction, out int nextIdx))
                    {
                        continue;
                    }

                    targetIdx = nextIdx;
                }

                if (targetIdx == -1 || targetIdx == floorIdx)
                {
                    continue;
                }

                (int targetX, int targetY) = _ctx.Map.GetTileXY(targetIdx);

                if (
                    await _ctx.Furni.ValidateFloorItemPlacementAsync(
                        ActionContext.Wired,
                        floorItem.ObjectId,
                        targetX,
                        targetY,
                        floorItem.Rotation
                    )
                )
                {
                    await ctx.ProcessFloorItemMovementAsync(
                        floorItem,
                        targetIdx,
                        null,
                        floorItem.Rotation
                    );
                }
            }
            catch
            {
                continue;
            }
        }

        return true;
    }

    private int GetAwayTileIdx(int fromIdx, int threatIdx)
    {
        int width = _ctx.Map.Width;
        int fx = fromIdx % width;
        int fy = fromIdx / width;

        int tx = threatIdx % width;
        int ty = threatIdx / width;

        int dx = tx - fx;
        int dy = ty - fy;

        // Step one tile in the direction that increases distance from the threat (opposite of chase).
        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            if (dx > 0)
            {
                return fromIdx - 1;
            }

            if (dx < 0)
            {
                return fromIdx + 1;
            }
        }

        if (dy > 0)
        {
            return fromIdx - width;
        }

        if (dy < 0)
        {
            return fromIdx + width;
        }

        return fromIdx;
    }
}
