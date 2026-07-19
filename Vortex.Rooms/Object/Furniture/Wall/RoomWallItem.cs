using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object.Furniture.Wall;
using Vortex.Primitives.Rooms.Object.Logic.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Furniture;

namespace Vortex.Rooms.Object.Furniture.Wall;

public class RoomWallItem
    : RoomItem<IRoomWallItem, IFurnitureWallLogic, IRoomWallItemContext>,
        IRoomWallItem
{
    public int WallOffset { get; private set; }

    public void SetWallOffset(int wallOffset)
    {
        if (WallOffset == wallOffset)
        {
            return;
        }

        WallOffset = wallOffset;

        MarkDirty();
    }

    public new RoomWallItemSnapshot GetSnapshot() => (RoomWallItemSnapshot)base.GetSnapshot();

    public override IComposer GetAddComposer() =>
        new ItemAddMessageComposer { WallItem = GetSnapshot() };

    public override IComposer GetUpdateComposer() =>
        new ItemUpdateMessageComposer { WallItem = GetSnapshot() };

    public virtual IComposer GetRefreshStateComposer() =>
        new ItemStateUpdateMessageComposer
        {
            ObjectId = ObjectId,
            State = Logic.StuffData.GetLegacyString(),
        };

    public override IComposer GetRefreshStuffDataComposer() =>
        new ItemDataUpdateMessageComposer
        {
            ObjectId = ObjectId,
            State = Logic.StuffData.GetLegacyString(),
        };

    public override IComposer GetRemoveComposer(
        PlayerId pickerId,
        bool isExpired = false,
        int delay = 0
    ) => new ItemRemoveMessageComposer { ObjectId = ObjectId, PickerId = pickerId };

    public string ConvertWallPositionToString() =>
        $":w={X},{Y} l={WallOffset},{Z} {(Rotation == Rotation.South ? "l" : "r")}";

    protected override RoomItemSnapshot BuildSnapshot() =>
        new RoomWallItemSnapshot()
        {
            ObjectId = ObjectId,
            OwnerId = OwnerId,
            OwnerName = OwnerName,
            DefinitionId = Definition.Id,
            SpriteId = Definition.SpriteId,
            X = X,
            Y = Y,
            Z = Z,
            Rotation = Rotation,
            StackHeight = Logic.GetStackHeight(),
            StuffData = Logic.StuffData.GetSnapshot(),
            ExtraData = ExtraData.GetJsonString(),
            UsagePolicy = Logic.GetUsagePolicy(),
            WallOffset = WallOffset,
            WallPosition = ConvertWallPositionToString(),
        };
}
