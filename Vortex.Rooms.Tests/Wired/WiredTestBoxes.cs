using System;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Furniture;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Object.Logic.Furniture;
using Vortex.Tests.Support;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The two stubs the wired engine suites build their rooms out of: a floor item that carries a logic,
/// and the context a logic's base class needs to exist.
/// </summary>
/// <remarks>
/// The logic under test never uses the context — every subclass here overrides the hydration hook —
/// but <c>FurnitureLogic</c>'s constructor reads the definition's stuff-data type and the object's
/// extra data to build its stuff data, so both have to be real enough to survive construction.
/// </remarks>
internal static class WiredTestBoxes
{
    public static IRoomFloorItem FloorItem(int objectId, IFurnitureLogic logic)
    {
        RoomObjectId id = new(objectId);

        return FakeProxy.Create<IRoomFloorItem>(call =>
            call.Method.Name switch
            {
                $"get_{nameof(IRoomFloorItem.ObjectId)}" => id,
                $"get_{nameof(IRoomFloorItem.Logic)}" => logic,
                _ => null,
            }
        );
    }

    /// <summary>
    /// A context for a box sitting on <paramref name="tileIdx"/> with object id
    /// <paramref name="objectId"/>. A logic reads its own id and tile from its context rather than
    /// from the item that carries it, so the two have to agree here the same way they do in a room.
    /// </summary>
    public static IRoomFloorItemContext Context(
        int objectId = 0,
        int tileIdx = 0,
        ExtraData? extraData = null
    )
    {
        // RoomObject is typed as the floor item itself on a floor context, not as the plain
        // IRoomObject the name suggests. A box whose configuration matters to the test supplies its
        // own extra data, so hydration reads it the way it reads a real box's.
        ExtraData carried = extraData ?? new ExtraData(null);
        IRoomFloorItem roomObject = FakeProxy.Create<IRoomFloorItem>(call =>
            call.Method.Name == "get_ExtraData" ? carried : null
        );

        // Nothing under test calls into these, but a wired box lights itself up when it fires -
        // ScheduleFlashRevert then a stuff-data write - so the context has to answer for that much
        // or every firing test dies in the flash instead of in what it is about.
        IRoomFurniAccess furni = FakeProxy.Create<IRoomFurniAccess>(_ => null);

        return FakeProxy.Create<IRoomFloorItemContext>(call =>
            call.Method.Name switch
            {
                nameof(IRoomFloorItemContext.GetTileIdx) => tileIdx,
                "get_ObjectId" => new RoomObjectId(objectId),
                "get_Definition" => Definition,
                "get_RoomObject" => roomObject,
                "get_Furni" => furni,
                "get_WiredLimits" => Limits,
                _ => call.Method.ReturnType == typeof(Task) ? Task.CompletedTask : null,
            }
        );
    }

    /// <summary>
    /// The tuning knobs a box reads while hydrating.
    /// </summary>
    /// <remarks>
    /// Answering these is what lets a suite put a <em>real</em> wired logic in a fake room. Without
    /// them <c>RepairIntParams</c> dereferences a null on the way through hydration, the trigger
    /// index catches it and skips the box — so the room comes back with no triggers and the test
    /// passes or fails for a reason that has nothing to do with what it is about.
    /// </remarks>
    private static readonly IWiredLimits Limits = FakeProxy.Create<IWiredLimits>(call =>
        call.Method.Name switch
        {
            "get_WiredSelectorMaxAreaSize" => 64,
            "get_WiredSelectedItemsLimit" => 32,
            "get_WiredNeighborhoodRadius" => 3,
            "get_WiredMaxIntParams" => 16,
            "get_WiredAllowWallFurni" => true,
            _ => null,
        }
    );

    /// <summary>A plain legacy-key floor definition. Nothing under test reads any field but the
    /// stuff-data type, which the logic base class needs to build its stuff data.</summary>
    private static readonly FurnitureDefinitionSnapshot Definition = new()
    {
        Id = 1,
        SpriteId = 1,
        Name = "wired_test_box",
        ProductType = ProductType.Floor,
        FurniCategory = FurnitureCategory.Default,
        LogicName = "default_floor",
        TotalStates = 1,
        Width = 1,
        Length = 1,
        StackHeight = Altitude.FromInt(100),
        CanStack = true,
        CanWalk = false,
        CanSit = false,
        CanLay = false,
        CanRecycle = false,
        CanTrade = true,
        CanGroup = false,
        CanSell = true,
        UsagePolicy = FurnitureUsageType.Nobody,
        ExtraData = null,
        StuffDataType = StuffDataType.LegacyKey,
    };
}
