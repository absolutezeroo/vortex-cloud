using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Object.Logic.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Furniture;
using Vortex.Rooms.Tests.Support;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Furniture;

/// <summary>
/// Where a placed item leaves its id on the map.
/// </summary>
/// <remarks>
/// <para>
/// An item coming out of an inventory snapshot is materialised without coordinates, and placement
/// used to attach it first: <c>AttatchObjectAsync</c> registers the footprint from the coordinates
/// the item is carrying, so every new floor item was registered at (0, 0) before the real position
/// was applied and registered a second time. Nothing took the first one back — removal clears the
/// footprint the item is standing on — so tile (0, 0) kept the id, and through it the item's height
/// and its <c>FurnitureOccupied</c> flag, for the life of the activation.
/// </para>
/// <para>
/// The fix is the order, which is the order the furniture-swap path already used for this reason.
/// The assertion is the invariant rather than the ordering: the tiles holding an item's id are
/// exactly its footprint, and nowhere else.
/// </para>
/// </remarks>
public sealed class FloorItemPlacementFootprintTests
{
    private const int TargetX = 5;
    private const int TargetY = 6;

    private static readonly RoomObjectId Placed = new(31);

    [Fact]
    public async Task PlacingAFloorItem_LeavesItsIdOnItsFootprintAndNowhereElse()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        IRoomFloorItem item = MovableFloorItem(harness, width: 1, length: 1);

        bool placed = await harness
            .Grain.FurniModule.PlaceFloorItemAsync(
                harness.ContextFor(RoomHarness.Owner),
                item,
                TargetX,
                TargetY,
                Rotation.North,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        placed.Should().BeTrue();

        TilesHolding(harness, Placed)
            .Should()
            .Equal(harness.Grain.MapModule.ToIdx(TargetX, TargetY));
    }

    /// <summary>
    /// The multi-tile case, which is the one that used to poison a whole corner rather than a single
    /// tile: a 2x2 registered four tiles around the origin on its way past.
    /// </summary>
    [Fact]
    public async Task PlacingAMultiTileItem_RegistersFourTilesAndOnlyThose()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        IRoomFloorItem item = MovableFloorItem(harness, width: 2, length: 2);

        await harness
            .Grain.FurniModule.PlaceFloorItemAsync(
                harness.ContextFor(RoomHarness.Owner),
                item,
                TargetX,
                TargetY,
                Rotation.North,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        List<int> footprint = TilesHolding(harness, Placed);

        footprint.Should().HaveCount(4);
        footprint.Should().Contain(harness.Grain.MapModule.ToIdx(TargetX, TargetY));
        footprint.Should().NotContain(0, "the origin is not part of this item's footprint");
    }

    private static List<int> TilesHolding(RoomHarness harness, RoomObjectId objectId) =>
        [
            .. Enumerable
                .Range(0, harness.Grain._state.TileFloorStacks.Length)
                .Where(idx => harness.Grain._state.TileFloorStacks[idx].Contains(objectId)),
        ];

    /// <summary>
    /// A floor item whose coordinates actually move when the room sets them — the point of the test
    /// is which coordinates the map reads and when, so those cannot be a constant.
    /// </summary>
    private static IRoomFloorItem MovableFloorItem(RoomHarness harness, int width, int length)
    {
        int x = 0;
        int y = 0;
        Rotation rotation = Rotation.North;
        Altitude z = Altitude.Zero;
        IRoomObjectLogic? attached = null;

        // Handed over by the provider during the attach, the way a real one is -- an item that
        // arrives with its logic already on it is refused by AttatchLogicAsync and never reaches
        // the map, which would make this test pass on its own.
        harness.LogicFactory = () =>
            FakeProxy.Create<IFurnitureFloorLogic>(call =>
                call.Method.Name switch
                {
                    nameof(IFurnitureFloorLogic.CanWalk) => true,
                    nameof(IFurnitureFloorLogic.CanStack) => true,
                    nameof(IFurnitureFloorLogic.CanSit) => false,
                    nameof(IFurnitureFloorLogic.CanLay) => false,
                    nameof(IFurnitureFloorLogic.GetPostureOffset) => Altitude.Zero,
                    _ => null,
                }
            );

        return FakeProxy.Create<IRoomFloorItem>(call =>
        {
            switch (call.Method.Name)
            {
                case nameof(IRoomFloorItem.SetPosition):
                    x = (int)call.Args![0]!;
                    y = (int)call.Args![1]!;

                    return null;
                case nameof(IRoomFloorItem.SetPositionZ):
                    z = (Altitude)call.Args![0]!;

                    return null;
                case nameof(IRoomFloorItem.SetRotation):
                    rotation = (Rotation)call.Args![0]!;

                    return null;
                case nameof(IRoomFloorItem.SetLogic):
                    attached = (IRoomObjectLogic)call.Args![0]!;

                    return null;
                default:
                    return call.Method.Name switch
                    {
                        $"get_{nameof(IRoomFloorItem.ObjectId)}" => Placed,
                        $"get_{nameof(IRoomFloorItem.OwnerId)}" => RoomHarness.Owner,
                        $"get_{nameof(IRoomFloorItem.X)}" => x,
                        $"get_{nameof(IRoomFloorItem.Y)}" => y,
                        $"get_{nameof(IRoomFloorItem.Z)}" => z,
                        $"get_{nameof(IRoomFloorItem.Height)}" => z,
                        $"get_{nameof(IRoomFloorItem.Rotation)}" => rotation,
                        $"get_{nameof(IRoomFloorItem.Logic)}" => attached,
                        $"get_{nameof(IRoomFloorItem.Definition)}" => Definition(width, length),
                        _ => null,
                    };
            }
        });
    }

    private static FurnitureDefinitionSnapshot Definition(int width, int length) =>
        new()
        {
            Id = 1,
            SpriteId = 1,
            Name = "test_item",
            ProductType = ProductType.Floor,
            FurniCategory = FurnitureCategory.Default,
            LogicName = "default",
            TotalStates = 1,
            Width = width,
            Length = length,
            StackHeight = Altitude.FromInt(1),
            CanStack = true,
            CanWalk = true,
            CanSit = false,
            CanLay = false,
            CanRecycle = false,
            CanTrade = true,
            CanGroup = false,
            CanSell = true,
            UsagePolicy = FurnitureUsageType.Everybody,
            ExtraData = null,
            StuffDataType = StuffDataType.LegacyKey,
        };
}
