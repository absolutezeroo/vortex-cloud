using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Furniture;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;
using Vortex.Rooms.Wired;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// Habbo's wired has an else: a pile's "negative" actions run when its trigger fired and its
/// conditions did not hold. The room had no such branch, so the one negative action that existed
/// ran on success instead — inverted rather than missing, which only shows in a live room.
/// </summary>
public sealed class WiredNegativeBranchTests
{
    [Fact]
    public void ConditionsHeld_RunsTheOrdinaryActionsOnly()
    {
        IWiredAction ordinary = Action(negative: false);
        IWiredAction negative = Action(negative: true);

        WiredActionBranch
            .Select([ordinary, negative], conditionsPassed: true)
            .Should()
            .Equal(ordinary);
    }

    [Fact]
    public void ConditionsFailed_RunsTheNegativeActionsOnly()
    {
        IWiredAction ordinary = Action(negative: false);
        IWiredAction negative = Action(negative: true);

        WiredActionBranch
            .Select([ordinary, negative], conditionsPassed: false)
            .Should()
            .Equal(negative);
    }

    [Fact]
    public void ConditionsFailed_WithNoNegativeAction_RunsNothing()
    {
        // The overwhelmingly common pile: this has to stay exactly as it was.
        WiredActionBranch
            .Select([Action(negative: false)], conditionsPassed: false)
            .Should()
            .BeEmpty();
    }

    [Fact]
    public void TheOrderOfTheBranchIsKept()
    {
        IWiredAction first = Action(negative: false);
        IWiredAction second = Action(negative: false);

        WiredActionBranch
            .Select([first, Action(negative: true), second], conditionsPassed: true)
            .Should()
            .Equal(first, second);
    }

    [Fact]
    public async Task CallStacks_AsksTheRoomToRunTheSelectedPiles()
    {
        FakeFurniAccess furni = new();
        TestCallStacks box = new(StubContext(furni));

        WiredSelectionSet selection = new();
        selection.SelectedFurniIds.Add(41);
        selection.SelectedFurniIds.Add(42);
        selection.SelectedPlayerIds.Add(7);

        await box.ExecuteAsync(Execution(selection), CancellationToken.None);

        furni.StacksCalled.Should().HaveCount(1);
        furni.StacksCalled[0].TargetFurniIds.Should().BeEquivalentTo([41, 42]);

        // The user who set the whole thing off travels with the call, or a called pile could not
        // act on "the user who walked on".
        furni.StacksCalled[0].InheritedPlayerIds.Should().BeEquivalentTo([7]);
    }

    [Fact]
    public async Task CallStacks_WithNothingSelected_AsksNothing()
    {
        FakeFurniAccess furni = new();
        TestCallStacks box = new(StubContext(furni));

        await box.ExecuteAsync(Execution(new WiredSelectionSet()), CancellationToken.None);

        furni.StacksCalled.Should().BeEmpty();
    }

    [Fact]
    public void TheNegativeCallAndSendSignal_DeclareTheirBranch()
    {
        FakeFurniAccess furni = new();

        new TestNegativeCallStacks(StubContext(furni)).IsNegative().Should().BeTrue();
        new TestCallStacks(StubContext(furni)).IsNegative().Should().BeFalse();
    }

    // ---- harness -------------------------------------------------------------------------------

    private static IWiredAction Action(bool negative) =>
        FakeProxy.Create<IWiredAction>(call => call.Method.Name == "IsNegative" ? negative : null);

    private static IWiredExecutionContext Execution(WiredSelectionSet selection) =>
        FakeProxy.Create<IWiredExecutionContext>(call =>
            call.Method.Name switch
            {
                "GetEffectiveSelectionAsync" => Task.FromResult<IWiredSelectionSet>(selection),
                "get_Selected" => selection,
                _ => null,
            }
        );

    private static IRoomFloorItemContext StubContext(IRoomFurniAccess furni)
    {
        FurnitureDefinitionSnapshot definition = new()
        {
            Id = 1,
            SpriteId = 1,
            Name = "wf_act_call_stacks",
            ProductType = ProductType.Floor,
            FurniCategory = FurnitureCategory.Default,
            LogicName = "wf_act_call_stacks",
            TotalStates = 1,
            Width = 1,
            Length = 1,
            StackHeight = default,
            CanStack = false,
            CanWalk = false,
            CanSit = false,
            CanLay = false,
            CanRecycle = false,
            CanTrade = false,
            CanGroup = false,
            CanSell = false,
            UsagePolicy = FurnitureUsageType.Everybody,
            ExtraData = null,
            StuffDataType = StuffDataType.LegacyKey,
        };

        IRoomFloorItem item = FakeProxy.Create<IRoomFloorItem>(call =>
            call.Method.Name == "get_ExtraData" ? new ExtraData(null) : null
        );

        return FakeProxy.Create<IRoomFloorItemContext>(call =>
            call.Method.Name switch
            {
                "get_Definition" => definition,
                "get_RoomObject" => item,
                "get_Furni" => furni,
                _ => null,
            }
        );
    }

    private sealed class TestCallStacks : WiredActionCallStacks
    {
        public TestCallStacks(IRoomFloorItemContext ctx)
            : base(null!, new StuffDataFactory(), ctx) => _wiredData = new WiredData();
    }

    private sealed class TestNegativeCallStacks : WiredActionCallStacksNegative
    {
        public TestNegativeCallStacks(IRoomFloorItemContext ctx)
            : base(null!, new StuffDataFactory(), ctx) => _wiredData = new WiredData();
    }
}
