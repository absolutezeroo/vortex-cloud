using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Action;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Enums.Wired;
using Turbo.Primitives.Rooms.Object.Avatars;
using Turbo.Primitives.Rooms.Object.Furniture;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;
using Turbo.Primitives.Rooms.Wired;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

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
                    !_roomGrain._state.ItemsById.TryGetValue(furniId, out IRoomItem? item)
                    || item is not IRoomFloorItem floorItem
                )
                {
                    continue;
                }

                int floorIdx = _roomGrain.MapModule.ToIdx(floorItem.X, floorItem.Y);
                int nearestIdx = -1;
                int bestDistance = int.MaxValue;

                foreach (IRoomAvatar avatar in _roomGrain._state.AvatarsByObjectId.Values)
                {
                    if (avatar is not IRoomPlayer player)
                    {
                        continue;
                    }

                    int playerIdx = _roomGrain.MapModule.ToIdx(player.X, player.Y);
                    int distance = _roomGrain.MapModule.GetDistanceBetween(floorIdx, playerIdx);

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

                    if (
                        !_roomGrain.MapModule.TryGetTileInFront(
                            floorIdx,
                            direction,
                            out int nextIdx
                        )
                    )
                    {
                        continue;
                    }

                    targetIdx = nextIdx;
                }

                if (targetIdx == -1 || targetIdx == floorIdx)
                {
                    continue;
                }

                (int targetX, int targetY) = _roomGrain.MapModule.GetTileXY(targetIdx);

                if (
                    await _roomGrain.FurniModule.ValidateFloorItemPlacementAsync(
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
                        floorItem.Z,
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
        int width = _roomGrain.MapModule.Width;
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
