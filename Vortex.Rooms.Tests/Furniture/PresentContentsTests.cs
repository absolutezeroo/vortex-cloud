using System.Text.Json;
using FluentAssertions;
using Vortex.Furniture;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Furniture;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Snapshots.Furniture;
using Vortex.Rooms.Object.Logic.Furniture.Floor;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Furniture;

/// <summary>
/// A present keeps two secrets in one blob. What it is wrapped in has to reach the client, packed
/// into the floor item's <c>extra</c> field; what is inside must not, because everything in stuff
/// data is broadcast to the whole room and a present that announced its own contents would be no
/// present at all.
/// </summary>
public sealed class PresentContentsTests
{
    [Fact]
    public void Wrapping_PacksTheWayTheVisualizationUnpacksIt()
    {
        // FurnitureGiftWrappedVisualization reads floor(extra / 1000) and extra % 1000 as two
        // separate sprite layers.
        int packed = FurniturePresentWrapping.Pack(boxType: 3, ribbonType: 7);

        packed.Should().Be(3007);
        FurniturePresentWrapping.Unpack(packed).Should().Be((3, 7));
    }

    [Fact]
    public void ReadContents_ComesBackFromThePersistedBlob()
    {
        IExtraData extraData = new ExtraData(null);

        extraData.UpdateSection(
            ExtraDataSectionType.PRESENT,
            new PresentContentsSnapshot
            {
                OfferId = 4242,
                ExtraParam = "gold",
                Wrapping = FurniturePresentWrapping.Pack(2, 5),
            }
        );

        FurniturePresentLogic logic = Build(new ExtraData(extraData.GetJsonString()));

        PresentContentsSnapshot? contents = logic.ReadContents();

        contents.Should().NotBeNull();
        contents!.OfferId.Should().Be(4242);
        contents.ExtraParam.Should().Be("gold");

        // The wrapping reaches the client through `extra`, which the serializer takes from the logic.
        logic.GetExtra().Should().Be(2005);
    }

    [Fact]
    public void ReadContents_IsNullForAPresentThatHoldsNothing()
    {
        // A row from before gifts were wrapped, or one placed by the furni editor. The opener has to
        // be able to tell this apart from a present holding offer 0, or it eats the furniture and
        // hands back nothing.
        FurniturePresentLogic logic = Build(new ExtraData(null));

        logic.ReadContents().Should().BeNull();
        logic.GetExtra().Should().Be(0);
    }

    [Fact]
    public void ReadContents_SurvivesTheCamelCasedWriter()
    {
        // ExtraDataWriter stores sections camel-cased while the record declares PascalCase, and its
        // members are required -- read case-sensitively this throws and the present looks empty.
        IExtraData extraData = new ExtraData(
            """{"present":{"offerId":9,"extraParam":"","wrapping":1001}}"""
        );

        FurniturePresentLogic logic = Build(extraData);

        logic.ReadContents().Should().NotBeNull();
        logic.ReadContents()!.OfferId.Should().Be(9);
    }

    [Fact]
    public void ReadContents_TreatsAMalformedSectionAsEmptyRatherThanThrowing()
    {
        // Hand-edited extra data reaches the room grain on load; throwing here would take the whole
        // room down rather than one unopenable box.
        FurniturePresentLogic logic = Build(new ExtraData("""{"present":"nonsense"}"""));

        logic.Invoking(l => l.ReadContents()).Should().NotThrow<JsonException>();
        logic.ReadContents().Should().BeNull();
    }

    private static FurniturePresentLogic Build(IExtraData extraData) =>
        new(new StuffDataFactory(), StubContext(extraData));

    private static IRoomFloorItemContext StubContext(IExtraData extraData)
    {
        FurnitureDefinitionSnapshot definition = new()
        {
            Id = 188,
            SpriteId = 188,
            Name = "present_gen1",
            ProductType = ProductType.Floor,
            FurniCategory = FurnitureCategory.Default,
            LogicName = "furniture_present",
            TotalStates = 1,
            Width = 1,
            Length = 1,
            StackHeight = default,
            CanStack = true,
            CanWalk = false,
            CanSit = false,
            CanLay = false,
            CanRecycle = false,
            CanTrade = true,
            CanGroup = false,
            CanSell = true,
            UsagePolicy = FurnitureUsageType.Everybody,
            ExtraData = null,
            StuffDataType = StuffDataType.MapKey,
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
