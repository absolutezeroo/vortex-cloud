using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Logging.Extensions;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Events.RoomItem;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

[RoomObjectLogic("wf_act_chase")]
public class WiredActionChaseHabbo(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.CHASE;

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

                bool didCollide = false;
                int bestTileIdx = -1;
                int bestDistance = int.MaxValue;
                int targetIdx = -1;
                int floorIdx = _ctx.Map.ToIdx(floorItem.X, floorItem.Y);

                foreach (IRoomAvatar avatar in _ctx.Lookup.Avatars)
                {
                    if (avatar is not IRoomPlayer player)
                    {
                        continue;
                    }

                    int playerIdx = _ctx.Map.ToIdx(player.X, player.Y);
                    int distance = _ctx.Map.GetDistanceBetween(floorIdx, playerIdx);

                    if (distance <= 1)
                    {
                        didCollide = true;

                        _ctx.PublishRoomEventAsync(
                                new RoomItemCollisionEvent()
                                {
                                    ObjectId = floorItem.ObjectId,
                                    CausedBy = ActionContext.CreateForPlayer(
                                        player.PlayerId,
                                        _ctx.RoomId
                                    ),
                                    RoomId = _ctx.RoomId,
                                },
                                ct
                            )
                            .LogAndForget(_logger, "Failed to publish room item collision event.");

                        break;
                    }

                    if (distance > 3)
                    {
                        continue;
                    }

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestTileIdx = playerIdx;
                    }
                }

                if (didCollide)
                {
                    continue;
                }

                if (bestTileIdx > -1)
                {
                    targetIdx = GetTargetTileIdx(floorIdx, bestTileIdx);
                }
                else
                {
                    Rotation direction = RotationExtensions.CARDINAL[Random.Shared.Next(0, 4)];

                    if (
                        _ctx.Map.TryGetTileInFront(
                            _ctx.Map.ToIdx(floorItem.X, floorItem.Y),
                            direction,
                            out int nextIdx
                        )
                    )
                    {
                        targetIdx = nextIdx;
                    }
                }

                if (targetIdx == -1)
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

    private int GetTargetTileIdx(int fromIdx, int toIdx)
    {
        int fx = fromIdx % _ctx.Map.Width;
        int fy = fromIdx / _ctx.Map.Width;

        int tx = toIdx % _ctx.Map.Width;
        int ty = toIdx / _ctx.Map.Width;

        int dx = tx - fx;
        int dy = ty - fy;

        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            if (dx > 0)
            {
                return fromIdx + 1;
            }

            if (dx < 0)
            {
                return fromIdx - 1;
            }
        }

        if (dy > 0)
        {
            return fromIdx + _ctx.Map.Width;
        }

        if (dy < 0)
        {
            return fromIdx - _ctx.Map.Width;
        }

        return fromIdx;
    }
}
