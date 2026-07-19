using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
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
                    !_roomGrain._state.ItemsById.TryGetValue(furniId, out IRoomItem? item)
                    || item is not IRoomFloorItem floorItem
                )
                {
                    continue;
                }

                bool didCollide = false;
                int bestTileIdx = -1;
                int bestDistance = int.MaxValue;
                int targetIdx = -1;
                int floorIdx = _roomGrain.MapModule.ToIdx(floorItem.X, floorItem.Y);

                foreach (IRoomAvatar avatar in _roomGrain._state.AvatarsByObjectId.Values)
                {
                    if (avatar is not IRoomPlayer player)
                    {
                        continue;
                    }

                    int playerIdx = _roomGrain.MapModule.ToIdx(player.X, player.Y);
                    int distance = _roomGrain.MapModule.GetDistanceBetween(floorIdx, playerIdx);

                    if (distance <= 1)
                    {
                        didCollide = true;

                        _ = _ctx.PublishRoomEventAsync(
                            new RoomItemCollisionEvent()
                            {
                                ObjectId = floorItem.ObjectId,
                                CausedBy = ActionContext.CreateForPlayer(
                                    player.PlayerId,
                                    _roomGrain.RoomId
                                ),
                                RoomId = _roomGrain.RoomId,
                            },
                            ct
                        );

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
                        _roomGrain.MapModule.TryGetTileInFront(
                            _roomGrain.MapModule.ToIdx(floorItem.X, floorItem.Y),
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

    private int GetTargetTileIdx(int fromIdx, int toIdx)
    {
        int fx = fromIdx % _roomGrain.MapModule.Width;
        int fy = fromIdx / _roomGrain.MapModule.Width;

        int tx = toIdx % _roomGrain.MapModule.Width;
        int ty = toIdx / _roomGrain.MapModule.Width;

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
            return fromIdx + _roomGrain.MapModule.Width;
        }

        if (dy < 0)
        {
            return fromIdx - _roomGrain.MapModule.Width;
        }

        return fromIdx;
    }
}
