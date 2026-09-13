using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Action;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;
using Vortex.Rooms.Wired;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The two boxes that let a stack ask what a wired chest still holds.
/// <para>
/// A room could already pay furniture and credits out of a chest and had no way to check one first,
/// so a prize machine kept firing on an empty chest: trigger flashes, nothing comes out, and nothing
/// anywhere says why. These pin the comparison the client's form sends and — just as importantly —
/// which furni slot each box reads its chests out of, because the two boxes share a form and swap
/// the slots around.
/// </para>
/// </summary>
public sealed class WiredChestConditionTests
{
    private static readonly RoomId Room = new(1);

    private const int Chest = 10;

    private const int TypeExample = 20;

    [Theory]
    // comparison code, held, amount, expected. The codes are the client's radio values, which are
    // not in reading order: 0 <, 1 =, 2 >, 3 <=, 4 !=, 5 >=.
    [InlineData(0, 2, 5, true)]
    [InlineData(0, 5, 5, false)]
    [InlineData(1, 5, 5, true)]
    [InlineData(1, 4, 5, false)]
    [InlineData(2, 6, 5, true)]
    [InlineData(2, 5, 5, false)]
    [InlineData(3, 5, 5, true)]
    [InlineData(3, 6, 5, false)]
    [InlineData(4, 4, 5, true)]
    [InlineData(4, 5, 5, false)]
    [InlineData(5, 5, 5, true)]
    [InlineData(5, 4, 5, false)]
    public async Task HasItems_ComparesTheCountTheClientsOwnWay(
        int comparison,
        int held,
        int amount,
        bool expected
    )
    {
        FakeChestAccess chests = new(held);
        TestChestHasItems condition = new(
            Context(chests, Chest),
            Config(amount, comparison, chests: [Chest])
        );

        await condition.PrepareAsync(Trigger(), CancellationToken.None);

        condition.Evaluate(Trigger()).Should().Be(expected);
    }

    [Fact]
    public async Task HasItems_ReadsTheChestsOutOfTheFirstSlot()
    {
        FakeChestAccess chests = new(3);
        TestChestHasItems condition = new(
            Context(chests, Chest),
            Config(1, comparison: 5, chests: [Chest])
        );

        await condition.PrepareAsync(Trigger(), CancellationToken.None);

        chests.Counted.Should().HaveCount(1);
        chests.Counted[0].ChestIds.Should().Equal(Chest);

        // This box counts everything a chest holds; naming no types is what says so.
        chests.Counted[0].KindExampleIds.Should().BeEmpty();
    }

    [Fact]
    public async Task HasItemTypes_ReadsTypesFromSlotZeroAndChestsFromSlotOne()
    {
        FakeChestAccess chests = new(3);
        TestChestHasItemTypes condition = new(
            Context(chests, Chest, TypeExample),
            Config(1, comparison: 5, chests: [Chest], types: [TypeExample])
        );

        await condition.PrepareAsync(Trigger(), CancellationToken.None);

        chests.Counted.Should().HaveCount(1);
        chests.Counted[0].ChestIds.Should().Equal(Chest);
        chests.Counted[0].KindExampleIds.Should().Equal(TypeExample);
    }

    [Fact]
    public async Task AnAmountFromAVariable_CountsNothingAndDoesNotPass()
    {
        FakeChestAccess chests = new(1000);
        TestChestHasItems condition = new(
            Context(chests, Chest),
            // Source 1 is "take the amount from a wired variable", which nothing here reads.
            Config(1, comparison: 5, chests: [Chest], amountSource: 1)
        );

        await condition.PrepareAsync(Trigger(), CancellationToken.None);

        // Not merely a false answer: the chest is never asked, because a threshold nobody resolved
        // would empty a chest on a number that came from somewhere else.
        chests.Counted.Should().BeEmpty();
        condition.Evaluate(Trigger()).Should().BeFalse();
    }

    [Fact]
    public void WithoutAPreparedCount_TheConditionDoesNotPass()
    {
        FakeChestAccess chests = new(1000);

        new TestChestHasItems(Context(chests, Chest), Config(1, comparison: 5, chests: [Chest]))
            .Evaluate(Trigger())
            .Should()
            .BeFalse();
    }

    [Fact]
    public void BothBoxesDeclareEverySlotTheClientsPickerReads()
    {
        FakeChestAccess chests = new();

        // The client resolves the form's merged amount source through mergedSelections(), which is
        // [1, 0] on the plain box and [2, 0] on the types one — furni slot 1 and 2 respectively,
        // user slot 0 for both. An undeclared slot is not a missing feature: the picker indexes the
        // list it was handed, reads past the end, and the whole wired dialog dies with it.
        TestChestHasItems plain = new(Context(chests, Chest), Config(1, 1, [Chest]));
        TestChestHasItemTypes types = new(
            Context(chests, Chest, TypeExample),
            Config(1, 1, [Chest], [TypeExample])
        );

        plain.GetAllowedFurniSources().Should().HaveCount(2);
        types.GetAllowedFurniSources().Should().HaveCount(3);

        plain.GetAllowedPlayerSources().Should().HaveCount(1);
        types.GetAllowedPlayerSources().Should().HaveCount(1);

        plain
            .GetAllowedFurniSources()
            .Concat(types.GetAllowedFurniSources())
            .Should()
            .OnlyContain(sources => sources.Length > 0);
    }

    [Fact]
    public void TheTwoBoxesRouteOnTheClientsOwnCodes()
    {
        FakeChestAccess chests = new();

        new TestChestHasItems(Context(chests, Chest), Config(1, 1, [Chest]))
            .WiredCode.Should()
            .Be(45);

        new TestChestHasItemTypes(Context(chests, Chest), Config(1, 1, [Chest]))
            .WiredCode.Should()
            .Be(46);
    }

    private static WiredData Config(
        int amount,
        int comparison,
        List<int> chests,
        List<int>? types = null,
        int amountSource = 0
    ) =>
        new()
        {
            IntParams = [amount, amountSource, 0, comparison],
            // Slot 0 is the chests on the plain box and the item types on the other one, so the two
            // lists are filled the way each box reads them.
            StuffIds = types ?? chests,
            StuffIds2 = types is null ? [] : chests,
        };

    private static IWiredProcessingContext Trigger()
    {
        TestEvent evt = new()
        {
            RoomId = Room,
            CausedBy = ActionContext.CreateForPlayer(new Primitives.Players.PlayerId(7), Room),
        };

        return FakeProxy.Create<IWiredProcessingContext>(call =>
            call.Method.Name == "get_Event" ? evt : null
        );
    }

    /// <summary>The room the box stands in: the chests it reaches, and the furni whose ids its
    /// configuration is allowed to keep.</summary>
    private static IRoomFloorItemContext Context(
        IRoomChestAccess chests,
        params int[] itemsInTheRoom
    )
    {
        FakeRoomLookup lookup = new();

        foreach (int id in itemsInTheRoom)
        {
            RoomObjectId objectId = new(id);

            lookup.ItemsById[objectId] = FakeProxy.Create<IRoomItem>(call =>
                call.Method.Name == "get_ObjectId" ? objectId : null
            );
        }

        return WiredTestBoxes.Context(chests: chests, lookup: lookup);
    }

    private sealed record TestEvent : RoomEvent;

    private sealed class TestChestHasItems : WiredConditionChestHasItems
    {
        public TestChestHasItems(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx)
        {
            data.AttatchRules(GetIntParamRules());
            _wiredData = data;
        }
    }

    private sealed class TestChestHasItemTypes : WiredConditionChestHasItemTypes
    {
        public TestChestHasItemTypes(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx)
        {
            data.AttatchRules(GetIntParamRules());
            _wiredData = data;
        }
    }
}
