using System.Collections.Generic;
using Turbo.Logging;
using Turbo.Primitives;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Object;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;

namespace Turbo.Rooms.Grains.Modules;

public sealed partial class RoomMapModule
{
    private bool AddFloorItem(IRoomFloorItem item)
    {
        int tileIdx = ToIdx(item.X, item.Y);

        if (!InBounds(tileIdx))
        {
            throw new TurboException(TurboErrorCodeEnum.TileOutOfBounds);
        }

        if (
            GetTileIdForSize(
                item.X,
                item.Y,
                item.Rotation,
                item.Definition.Width,
                item.Definition.Length,
                out List<int> tileIds
            )
        )
        {
            foreach (int idx in tileIds)
            {
                _roomGrain._state.TileFloorStacks[idx].Add(item.ObjectId);

                ComputeTile(idx);
            }
        }

        return true;
    }

    public bool PlaceFloorItem(IRoomFloorItem item, int nTileIdx, Rotation rot)
    {
        if (!InBounds(nTileIdx))
        {
            throw new TurboException(TurboErrorCodeEnum.TileOutOfBounds);
        }

        (int targetX, int targetY) = GetTileXY(nTileIdx);

        item.SetPosition(targetX, targetY);
        item.SetPositionZ(_roomGrain._state.TileHeights[nTileIdx]);
        item.SetRotation(rot);

        return AddFloorItem(item);
    }

    public bool MoveFloorItem(
        IRoomFloorItem item,
        int tileIdx,
        Altitude? z = null,
        Rotation? targetRot = null
    )
    {
        if (!InBounds(tileIdx))
        {
            throw new TurboException(TurboErrorCodeEnum.TileOutOfBounds);
        }

        (int sourceX, int sourceY, Rotation sourceRot) = (item.X, item.Y, item.Rotation);
        (int targetX, int targetY) = GetTileXY(tileIdx);
        Altitude finalZ =
            z
            ?? (
                sourceX != targetX || sourceY != targetY
                    ? _roomGrain._state.TileHeights[tileIdx]
                    : item.Z
            );
        Rotation finalRot = targetRot ?? sourceRot;

        if (sourceX != targetX || sourceY != targetY || sourceRot != finalRot)
        {
            RemoveFloorItem(item);

            item.SetPosition(targetX, targetY);
            item.SetPositionZ(finalZ);
            item.SetRotation(finalRot);

            AddFloorItem(item);
        }
        else
        {
            item.SetPositionZ(finalZ);

            ComputeTile(tileIdx);
        }

        return true;
    }

    public bool RollFloorItem(IRoomFloorItem item, int tileIdx, Altitude z)
    {
        if (!InBounds(tileIdx))
        {
            throw new TurboException(TurboErrorCodeEnum.TileOutOfBounds);
        }

        RemoveFloorItem(item);

        (int targetX, int targetY) = GetTileXY(tileIdx);

        item.SetPosition(targetX, targetY);
        item.SetPositionZ(z);

        AddFloorItem(item);

        return true;
    }

    public bool RemoveFloorItem(IRoomFloorItem item)
    {
        int tileIdx = ToIdx(item.X, item.Y);

        if (!InBounds(tileIdx))
        {
            throw new TurboException(TurboErrorCodeEnum.TileOutOfBounds);
        }

        if (
            GetTileIdForSize(
                item.X,
                item.Y,
                item.Rotation,
                item.Definition.Width,
                item.Definition.Length,
                out List<int> tileIds
            )
        )
        {
            foreach (int idx in tileIds)
            {
                _roomGrain._state.TileFloorStacks[idx].Remove(item.ObjectId);

                ComputeTile(idx);
            }
        }

        return true;
    }
}
