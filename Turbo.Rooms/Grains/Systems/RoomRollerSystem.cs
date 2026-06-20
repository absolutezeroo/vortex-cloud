using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Messages.Outgoing.Room.Engine;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Rooms;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Events;
using Turbo.Primitives.Rooms.Events.RoomItem;
using Turbo.Primitives.Rooms.Object;
using Turbo.Primitives.Rooms.Object.Avatars;
using Turbo.Primitives.Rooms.Object.Furniture;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Snapshots;
using Turbo.Rooms.Object.Logic.Furniture.Floor;

namespace Turbo.Rooms.Grains.Systems;

public sealed class RoomRollerSystem(RoomGrain roomGrain) : IRoomEventListener
{
    private readonly List<List<int>> _rollerIdSets = [];
    private readonly RoomGrain _roomGrain = roomGrain;

    private bool _isDirtyRollers = true;

    public Task OnRoomEventAsync(RoomEvent evt, CancellationToken ct)
    {
        return HandleRoomEventAsync(evt, ct);
    }

    public Task ProcessRollersAsync(long now, CancellationToken ct)
    {
        if (now < _roomGrain._state.NextRollerBoundaryMs)
        {
            return Task.CompletedTask;
        }

        while (now >= _roomGrain._state.NextRollerBoundaryMs)
        {
            _roomGrain._state.NextRollerBoundaryMs += _roomGrain._roomConfig.RollerTickMs;
        }

        ComputeRollers();

        if (_rollerIdSets.Count == 0)
        {
            return Task.CompletedTask;
        }

        List<RollerMovePlan> currentPlans = new();
        HashSet<int> reservedTileIdxs = new();
        HashSet<int> nextAvatarTiles = new(
            _roomGrain
                ._state.AvatarsByObjectId.Values.Where(x => x.NextTileId >= 0)
                .Select(x => x.NextTileId)
        );

        foreach (List<int> rollerIds in _rollerIdSets)
        {
            if (rollerIds.Count == 0)
            {
                continue;
            }

            foreach (int rollerId in rollerIds)
            {
                try
                {
                    if (!_roomGrain._state.ItemsById.TryGetValue(rollerId, out IRoomItem? roller))
                    {
                        continue;
                    }

                    int fromIdx = _roomGrain.MapModule.ToIdx(roller.X, roller.Y);

                    if (
                        !_roomGrain.MapModule.TryGetTileInFront(
                            fromIdx,
                            roller.Rotation,
                            out int toIdx
                        )
                        || fromIdx == toIdx
                        || reservedTileIdxs.Contains(toIdx)
                        || nextAvatarTiles.Contains(toIdx)
                    )
                    {
                        continue;
                    }

                    RoomTileFlags toTileState = _roomGrain._state.TileFlags[toIdx];
                    Altitude toTileHeight = _roomGrain._state.TileHeights[toIdx];
                    Altitude rollerHeight = roller.Height;

                    if (
                        toTileHeight > rollerHeight
                        || toTileState.Has(RoomTileFlags.AvatarOccupied, RoomTileFlags.Disabled)
                    )
                    {
                        continue;
                    }

                    List<IRoomItem> items = new();
                    List<IRoomAvatar> avatars = new();
                    bool canAvatarMove = true;

                    foreach (RoomObjectId itemId in _roomGrain._state.TileFloorStacks[fromIdx])
                    {
                        if (
                            !_roomGrain._state.ItemsById.TryGetValue(itemId, out IRoomItem? item)
                            || item.Definition.Width > 1
                            || item.Definition.Length > 1
                            || item.Z < rollerHeight
                            || !item.Logic.CanRoll()
                            || toTileState.Has(RoomTileFlags.StackBlocked)
                        )
                        {
                            continue;
                        }

                        items.Add(item);
                    }

                    foreach (RoomObjectId avatarId in _roomGrain._state.TileAvatarStacks[fromIdx])
                    {
                        if (
                            !_roomGrain._state.AvatarsByObjectId.TryGetValue(
                                avatarId,
                                out IRoomAvatar? avatar
                            )
                            || avatar.Z < rollerHeight
                        )
                        {
                            continue;
                        }

                        if (
                            !avatar.Logic.CanRoll()
                            || !_roomGrain.MapModule.CanAvatarWalk(avatar, toIdx)
                        )
                        {
                            canAvatarMove = false;

                            break;
                        }

                        avatars.Add(avatar);
                    }

                    if (!canAvatarMove || (items.Count == 0 && avatars.Count == 0))
                    {
                        continue;
                    }

                    currentPlans.Add(
                        new RollerMovePlan
                        {
                            RollerId = roller.ObjectId,
                            FromIdx = fromIdx,
                            ToIdx = toIdx,
                            MovedFloorItems =
                            [
                                .. items.Select(x => new RollerMovedObject
                                {
                                    ObjectId = x.ObjectId,
                                    RoomObject = x,
                                    FromZ = x.Z,
                                    ToZ = x.Z - rollerHeight + toTileHeight
                                })
                            ],
                            MovedAvatars =
                            [
                                .. avatars.Select(x =>
                                {
                                    return new RollerMovedObject
                                    {
                                        ObjectId = x.ObjectId,
                                        RoomObject = x,
                                        FromZ = x.Z,
                                        ToZ = x.Z - rollerHeight + toTileHeight
                                    };
                                })
                            ]
                        }
                    );

                    reservedTileIdxs.Add(toIdx);
                }
                catch (Exception)
                {
                }
            }
        }

        if (currentPlans.Count == 0)
        {
            return Task.CompletedTask;
        }

        List<IComposer> composers = new();

        foreach (RollerMovePlan plan in currentPlans)
        {
            (int fromX, int fromY) = _roomGrain.MapModule.GetTileXY(plan.FromIdx);
            (int toX, int toY) = _roomGrain.MapModule.GetTileXY(plan.ToIdx);

            foreach (RollerMovedObject item in plan.MovedFloorItems)
            {
                _roomGrain.MapModule.RollFloorItem(
                    (IRoomFloorItem)item.RoomObject,
                    plan.ToIdx,
                    item.ToZ
                );
            }

            foreach (RollerMovedObject avatar in plan.MovedAvatars)
            {
                _roomGrain.MapModule.RollAvatar(
                    (IRoomAvatar)avatar.RoomObject,
                    plan.ToIdx,
                    avatar.ToZ
                );
            }

            if (plan.MovedAvatars.Count > 0)
            {
                bool sent = false;

                foreach (RollerMovedObject avatar in plan.MovedAvatars)
                {
                    IRoomAvatar avatarObject = (IRoomAvatar)avatar.RoomObject;

                    if (!sent)
                    {
                        composers.Add(
                            new SlideObjectBundleMessageComposer
                            {
                                FromX = fromX,
                                FromY = fromY,
                                ToX = toX,
                                ToY = toY,
                                RollerItemId = plan.RollerId,
                                FloorItemHeights =
                                [
                                    .. plan.MovedFloorItems.Select(x =>
                                        (x.RoomObject.ObjectId, x.FromZ, x.ToZ)
                                    )
                                ],
                                Avatar = (
                                    SlideAvatarMoveType.Slide,
                                    avatar.RoomObject.ObjectId,
                                    avatar.FromZ + avatarObject.PostureOffset,
                                    avatar.ToZ + avatarObject.PostureOffset
                                )
                            }
                        );

                        sent = true;

                        continue;
                    }

                    composers.Add(
                        new SlideObjectBundleMessageComposer
                        {
                            FromX = fromX,
                            FromY = fromY,
                            ToX = toX,
                            ToY = toY,
                            RollerItemId = plan.RollerId,
                            FloorItemHeights = [],
                            Avatar = (
                                SlideAvatarMoveType.Slide,
                                avatar.RoomObject.ObjectId,
                                avatar.FromZ + avatarObject.PostureOffset,
                                avatar.ToZ + avatarObject.PostureOffset
                            )
                        }
                    );
                }
            }
            else
            {
                composers.Add(
                    new SlideObjectBundleMessageComposer
                    {
                        FromX = fromX,
                        FromY = fromY,
                        ToX = toX,
                        ToY = toY,
                        RollerItemId = plan.RollerId,
                        FloorItemHeights =
                        [
                            .. plan.MovedFloorItems.Select(x =>
                                (x.RoomObject.ObjectId, x.FromZ, x.ToZ)
                            )
                        ],
                        Avatar = null
                    }
                );
            }
        }

        foreach (IComposer composer in composers)
        {
            _ = _roomGrain.SendComposerToRoomAsync(composer);
        }

        return Task.CompletedTask;
    }

    private void ComputeRollers()
    {
        if (!_isDirtyRollers || !_roomGrain._state.IsFurniLoaded)
        {
            return;
        }

        _rollerIdSets.Clear();

        List<IRoomItem> rollers = _roomGrain
            ._state.ItemsById.Values.Where(x => x.Logic is FurnitureRollerLogic)
            .ToList();

        if (rollers.Count == 0)
        {
            return;
        }

        foreach (
            IGrouping<Rotation, IRoomItem> group in rollers
                .GroupBy(x => x.Rotation)
                .OrderBy(x => x.Key)
        )
        {
            IEnumerable<IRoomItem> stack = OrderRollersFrontToBack(group);

            _rollerIdSets.Add([.. stack.Select(x => x.ObjectId)]);
        }

        _isDirtyRollers = false;
    }

    private Task HandleRoomEventAsync(RoomEvent evt, CancellationToken ct)
    {
        switch (evt)
        {
            case RoomRollerChangedEvent rollerChangedEvent:
                _isDirtyRollers = true;
                break;
            default:
                return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }

    private static IEnumerable<IRoomItem> OrderRollersFrontToBack(IEnumerable<IRoomItem> rollers)
    {
        List<IRoomItem> list = rollers.ToList();

        if (list.Count == 0)
        {
            return list;
        }

        Rotation dir = list[0].Rotation;

        return dir switch
        {
            Rotation.East => list.OrderByDescending(r => r.X).ThenBy(r => r.Y),
            Rotation.West => list.OrderBy(r => r.X).ThenBy(r => r.Y),
            Rotation.South => list.OrderByDescending(r => r.Y).ThenBy(r => r.X),
            Rotation.North => list.OrderBy(r => r.Y).ThenBy(r => r.X),
            _ => list.OrderBy(r => r.Y).ThenBy(r => r.X)
        };
    }
}
