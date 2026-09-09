using System.Collections.Generic;
using Vortex.Logging;
using Vortex.Primitives;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;

namespace Vortex.Rooms.Grains.Modules;

public sealed partial class RoomMapModule
{
    private bool AddFloorItem(IRoomFloorItem item)
    {
        int tileIdx = ToIdx(item.X, item.Y);

        if (!InBounds(tileIdx))
        {
            throw new VortexException(VortexErrorCodeEnum.TileOutOfBounds);
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

    /// <summary>
    /// Gives the item its resting place on <paramref name="nTileIdx" />, without registering it.
    /// </summary>
    /// <remarks>
    /// Positioning and registering are two steps on purpose, and the order between them is the
    /// point: <c>AddFloorItem</c> reads the item's own coordinates, so it can only run once the
    /// item has them. This used to do both, after the caller had already attached the item — which
    /// registered the footprint at the default (0, 0) first, and left it there for good, because
    /// removal only ever clears the footprint the item is standing on.
    ///
    /// The Z read here is the tile's height before this item joins the pile, which is what makes it
    /// the surface the item rests on rather than its own top.
    /// </remarks>
    public void PositionFloorItem(IRoomFloorItem item, int nTileIdx, Rotation rot)
    {
        if (!InBounds(nTileIdx))
        {
            throw new VortexException(VortexErrorCodeEnum.TileOutOfBounds);
        }

        (int targetX, int targetY) = GetTileXY(nTileIdx);

        item.SetPosition(targetX, targetY);
        item.SetPositionZ(_roomGrain._state.TileHeights[nTileIdx]);
        item.SetRotation(rot);
    }

    /// <summary>
    /// Positions the item and registers the footprint it now stands on, in that order.
    /// </summary>
    /// <remarks>
    /// For an item already live in the room whose footprint has just been taken off the map. An
    /// item being attached must not come through here -- <c>AttatchObjectAsync</c> registers it, so
    /// it would be registered twice; that path positions first and lets the attach do the rest.
    /// </remarks>
    public bool PlaceFloorItem(IRoomFloorItem item, int nTileIdx, Rotation rot)
    {
        PositionFloorItem(item, nTileIdx, rot);

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
            throw new VortexException(VortexErrorCodeEnum.TileOutOfBounds);
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
            throw new VortexException(VortexErrorCodeEnum.TileOutOfBounds);
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
            throw new VortexException(VortexErrorCodeEnum.TileOutOfBounds);
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
