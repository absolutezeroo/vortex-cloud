using FluentAssertions;
using Vortex.Furniture;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Furniture;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Rooms.Object.Logic.Furniture.Floor;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Furniture;

/// <summary>
/// The magic tile's multi-walk flag is not stuff data and has no state number to live in: the wire
/// carries it in the floor item's <c>extra</c> field, which the client turns straight into the
/// widget's checkbox. It therefore needs its own extra-data section, and a flag written only to
/// memory would come back unticked the next time the room loaded.
/// </summary>
public sealed class CustomStackHeightMultiWalkTests
{
    [Fact]
    public void MultiWalk_DefaultsOffAndReportsNoExtra()
    {
        FurnitureCustomStackHeightLogic logic = new(
            new StuffDataFactory(),
            StubContext(new ExtraData(null))
        );

        logic.MultiWalk.Should().BeFalse();
        logic.GetExtra().Should().Be(0);
    }

    [Fact]
    public void SetMultiWalk_ReachesThePersistedExtraDataAndComesBack()
    {
        IExtraData extraData = new ExtraData(null);
        FurnitureCustomStackHeightLogic logic = new(new StuffDataFactory(), StubContext(extraData));

        logic.SetMultiWalk(true);

        logic.GetExtra().Should().Be(1);

        // The room reloading is the case that matters: the tile is rebuilt from the persisted
        // string, so a flag that never left memory would silently untick itself here.
        FurnitureCustomStackHeightLogic reloaded = new(
            new StuffDataFactory(),
            StubContext(new ExtraData(extraData.GetJsonString()))
        );

        reloaded.MultiWalk.Should().BeTrue();
        reloaded.GetExtra().Should().Be(1);
    }

    [Fact]
    public void SetMultiWalk_UntickingSurvivesTheSameRoundTrip()
    {
        IExtraData extraData = new ExtraData(null);
        FurnitureCustomStackHeightLogic logic = new(new StuffDataFactory(), StubContext(extraData));

        logic.SetMultiWalk(true);
        logic.SetMultiWalk(false);

        FurnitureCustomStackHeightLogic reloaded = new(
            new StuffDataFactory(),
            StubContext(new ExtraData(extraData.GetJsonString()))
        );

        reloaded.MultiWalk.Should().BeFalse();
        reloaded.GetExtra().Should().Be(0);
    }

    private static IRoomFloorItemContext StubContext(IExtraData extraData)
    {
        FurnitureDefinitionSnapshot definition = new()
        {
            Id = 5103,
            SpriteId = 5103,
            Name = "tile_stackmagic",
            ProductType = ProductType.Floor,
            FurniCategory = FurnitureCategory.Default,
            LogicName = "furniture_custom_stack_height",
            TotalStates = 0,
            Width = 1,
            Length = 1,
            StackHeight = default,
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

        IRoomFloorItem item = FakeProxy.Create<IRoomFloorItem>(call =>
            call.Method.Name == "get_ExtraData" ? extraData : null
        );

        return FakeProxy.Create<IRoomFloorItemContext>(call =>
            call.Method.Name switch
            {
                "get_Definition" => definition,
                "get_RoomObject" => item,
                _ => null,
            }
        );
    }
}
