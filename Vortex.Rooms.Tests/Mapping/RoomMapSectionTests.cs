using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Rooms.Mapping;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic.Furniture;
using Vortex.Rooms.Tests.Support;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Mapping;

/// <summary>
/// The same tile answering two different heights, through the real map module.
///
/// <see cref="RoomTileSectionFinderTests" /> pins the arithmetic down on plain numbers; this pins
/// down the half that arithmetic cannot see — that <c>CollectOccupants()</c> reads a real
/// <see cref="IRoomFloorItem" /> into the right slab. <c>Z</c> is where the item rests and
/// <c>Height</c> is <c>Z + stack height</c>: get those two the wrong way round and every surface in
/// the room is wrong, with nothing in the pure tests able to notice.
/// </summary>
public sealed class RoomMapSectionTests
{
    private const int Tile = 2;

    /// <summary>A floor item filling <c>[bottom, top]</c>, and nothing else the map asks about.</summary>
    private static IRoomFloorItem Platform(int objectId, double bottom, double top)
    {
        IFurnitureFloorLogic logic = FakeProxy.Create<IFurnitureFloorLogic>(call =>
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
            call.Method.Name switch
            {
                $"get_{nameof(IRoomFloorItem.ObjectId)}" => new RoomObjectId(objectId),
                $"get_{nameof(IRoomFloorItem.Z)}" => Altitude.FromValue(bottom),
                $"get_{nameof(IRoomFloorItem.Height)}" => Altitude.FromValue(top),
                $"get_{nameof(IRoomFloorItem.Logic)}" => logic,
                _ => null,
            }
        );
    }

    private static async Task<(RoomHarness Harness, int TileId)> RoomWithPlatformAsync(
        double bottom,
        double top
    )
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        int tileId = harness.Grain.MapModule.ToIdx(Tile, Tile);
        IRoomFloorItem platform = Platform(1, bottom, top);

        harness.Grain._state.ItemsById[platform.ObjectId] = platform;
        harness.Grain._state.TileFloorStacks[tileId].Add(platform.ObjectId);

        return (harness, tileId);
    }

    /// <summary>
    /// The feature itself. A platform resting two units up is walked *under* by somebody at floor
    /// level and walked *on* by somebody already up there — one tile, two answers, chosen by where
    /// the asker's feet are.
    /// </summary>
    [Fact]
    public async Task ARaisedPlatform_OffersTheFloorBelowAndItsOwnTop()
    {
        (RoomHarness harness, int tileId) = await RoomWithPlatformAsync(bottom: 2, top: 3)
            .ConfigureAwait(true);

        RoomTileSection? fromTheFloor = harness.Grain.MapModule.FindSection(
            tileId,
            Altitude.Zero,
            Altitude.FromValue(2)
        );

        fromTheFloor.Should().NotBeNull();
        fromTheFloor!.Value.Height.Should().Be(Altitude.Zero);
        fromTheFloor
            .Value.IsBareFloor.Should()
            .BeTrue("there is two units of air under the platform");

        RoomTileSection? fromAbove = harness.Grain.MapModule.FindSection(
            tileId,
            Altitude.FromValue(3),
            Altitude.FromValue(2)
        );

        fromAbove.Should().NotBeNull();
        fromAbove!.Value.Height.Should().Be(Altitude.FromValue(3));
        fromAbove.Value.ItemId.Value.Should().Be(1);
        fromAbove.Value.IsWalkable.Should().BeTrue();
    }

    /// <summary>
    /// One unit of headroom is not a crawlspace: the tile stops offering its floor, so the
    /// pathfinder routes around instead of walking somebody into it.
    /// </summary>
    [Fact]
    public async Task APlatformTooLow_ClosesTheTileToSomebodyOnTheFloor()
    {
        (RoomHarness harness, int tileId) = await RoomWithPlatformAsync(bottom: 1, top: 2)
            .ConfigureAwait(true);

        harness
            .Grain.MapModule.FindSection(tileId, Altitude.Zero, Altitude.FromValue(0))
            .Should()
            .BeNull();
    }

    /// <summary>
    /// A tile the walk cannot reach at any of its heights is not a step, which is the answer the
    /// pathfinder needs in order to go round a cliff rather than into it.
    /// </summary>
    [Fact]
    public async Task ASurfaceOutOfStep_IsNotOffered()
    {
        (RoomHarness harness, int tileId) = await RoomWithPlatformAsync(bottom: 8, top: 9)
            .ConfigureAwait(true);

        RoomTileSection? section = harness.Grain.MapModule.FindSection(
            tileId,
            Altitude.FromValue(9),
            Altitude.FromValue(2)
        );

        section.Should().NotBeNull("standing on the platform, its own top is under the foot");
        section!.Value.Height.Should().Be(Altitude.FromValue(9));

        harness
            .Grain.MapModule.FindSection(tileId, Altitude.FromValue(4), Altitude.FromValue(2))
            .Should()
            .BeNull("neither the floor nor the platform is within two units of 4");
    }
}
