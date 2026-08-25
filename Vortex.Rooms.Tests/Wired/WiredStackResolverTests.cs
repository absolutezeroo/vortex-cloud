using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic.Furniture;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Selectors;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Variables;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Engine;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// Resolving the pile stacked on a tile — the step that makes the "same pile" rule true, run here
/// without a room behind it.
/// </summary>
/// <remarks>
/// The pile is resolved live at fire time rather than cached, which is why a box dragged off the tile
/// stops driving it for free. These tests pin what "resolved" means: which boxes go in which bucket,
/// which ones are not pile members at all, and what a box that will not load costs.
/// </remarks>
public sealed class WiredStackResolverTests
{
    private const int TILE = 12;

    [Fact]
    public async Task EveryKindOfBoxLandsInItsOwnBucket()
    {
        FakeWiredRoomHost room = new();
        room.With(Box(1, new TestTrigger(1)), TILE);
        room.With(Box(2, new TestSelector(2)), TILE);
        room.With(Box(3, new TestCondition(3)), TILE);
        room.With(Box(4, new TestAddon(4)), TILE);
        room.With(Box(5, new TestAction(5)), TILE);

        WiredStack stack = await Resolve(room);

        stack.StackId.Should().Be(TILE);
        stack.Triggers.Should().ContainSingle();
        stack.Selectors.Should().ContainSingle();
        stack.Conditions.Should().ContainSingle();
        stack.Addons.Should().ContainSingle();
        stack.Actions.Should().ContainSingle();
    }

    /// <summary>
    /// Variable boxes are wired furniture but not pile members: they are read by the boxes that
    /// reference them, not run alongside them. Letting one into the pile would make it a member of
    /// every pile it happened to be stacked on.
    /// </summary>
    [Fact]
    public async Task AVariableBoxIsNotAPileMember()
    {
        FakeWiredRoomHost room = new();
        room.With(Box(1, new TestVariable(1)), TILE);
        room.With(Box(2, new TestAction(2)), TILE);

        WiredStack stack = await Resolve(room);

        stack.Actions.Should().ContainSingle();
        stack.Triggers.Should().BeEmpty();
        stack.Selectors.Should().BeEmpty();
        stack.Conditions.Should().BeEmpty();
        stack.Addons.Should().BeEmpty();
    }

    /// <summary>Furniture that is not a wired box at all is simply not in the pile.</summary>
    [Fact]
    public async Task PlainFurnitureOnTheTileIsIgnored()
    {
        FakeWiredRoomHost room = new();
        room.With(WiredTestBoxes.FloorItem(1, FakeProxy.Create<IFurnitureLogic>(_ => null)), TILE);
        room.With(Box(2, new TestAction(2)), TILE);

        (await Resolve(room)).Actions.Should().ContainSingle();
    }

    /// <summary>
    /// Effects run in object-id order, because the physical stacking order is not meaningful in
    /// Habbo and execution still has to be deterministic.
    /// </summary>
    [Fact]
    public async Task ActionsComeBackInObjectIdOrder()
    {
        FakeWiredRoomHost room = new();
        room.With(Box(30, new TestAction(30)), TILE);
        room.With(Box(10, new TestAction(10)), TILE);
        room.With(Box(20, new TestAction(20)), TILE);

        WiredStack stack = await Resolve(room);

        stack
            .Actions.Select(a => ((FurnitureWiredLogic)a).ObjectId.Value)
            .Should()
            .Equal([10, 20, 30]);
    }

    /// <summary>One box that will not load costs the pile that box, not the pile.</summary>
    [Fact]
    public async Task ABoxThatFailsToLoadIsSkippedAndTheRestOfThePileStands()
    {
        FakeWiredRoomHost room = new();
        room.With(Box(1, new TestAction(1, hydrationThrows: true)), TILE);
        room.With(Box(2, new TestAction(2)), TILE);

        WiredStack stack = await Resolve(room);

        stack
            .Actions.Should()
            .ContainSingle()
            .Which.Should()
            .BeAssignableTo<FurnitureWiredLogic>()
            .Which.ObjectId.Value.Should()
            .Be(2);
    }

    /// <summary>
    /// A box on another tile is not in this pile. This is the whole "same pile" rule, and it costs
    /// nothing because the pile is read live rather than cached.
    /// </summary>
    [Fact]
    public async Task ABoxOnAnotherTileIsNotInThisPile()
    {
        FakeWiredRoomHost room = new();
        room.With(Box(1, new TestAction(1)), TILE);
        room.With(Box(2, new TestAction(2)), TILE + 1);

        (await Resolve(room)).Actions.Should().ContainSingle();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public async Task ATileOutsideTheRoomResolvesToAnEmptyPile(int tileIdx)
    {
        FakeWiredRoomHost room = new();
        room.With(Box(1, new TestAction(1)), TILE);

        WiredStack stack = await new WiredStackResolver(
            room.View,
            room.Diagnostics
        ).BuildFromTileAsync(tileIdx, CancellationToken.None);

        stack.Actions.Should().BeEmpty();
        stack.Triggers.Should().BeEmpty();
    }

    [Fact]
    public async Task ABoxStillOnItsTileIsRecognised()
    {
        FakeWiredRoomHost room = new();
        room.With(Box(1, new TestAction(1)), TILE);

        WiredStackResolver resolver = new(room.View, room.Diagnostics);

        resolver.IsOnTile(new RoomObjectId(1), TILE).Should().BeTrue();
        resolver.IsOnTile(new RoomObjectId(1), TILE + 1).Should().BeFalse();
        resolver.IsOnTile(new RoomObjectId(2), TILE).Should().BeFalse();

        await Task.CompletedTask;
    }

    private static Task<WiredStack> Resolve(FakeWiredRoomHost room) =>
        new WiredStackResolver(room.View, room.Diagnostics).BuildFromTileAsync(
            TILE,
            CancellationToken.None
        );

    private static IRoomFloorItem Box(int objectId, FurnitureWiredLogic logic) =>
        WiredTestBoxes.FloorItem(objectId, logic);

    private static IGrainFactory Grains() => FakeProxy.Create<IGrainFactory>(_ => null);

    private static IStuffDataFactory Stuff() => FakeProxy.Create<IStuffDataFactory>(_ => null);

    private sealed class TestTrigger(int objectId)
        : FurnitureWiredTriggerLogic(Grains(), Stuff(), WiredTestBoxes.Context(objectId))
    {
        public override int WiredCode => 0;

        protected override Task FillInternalDataAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class TestSelector(int objectId)
        : FurnitureWiredSelectorLogic(Grains(), Stuff(), WiredTestBoxes.Context(objectId))
    {
        public override int WiredCode => 0;

        protected override Task FillInternalDataAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class TestCondition(int objectId)
        : FurnitureWiredConditionLogic(Grains(), Stuff(), WiredTestBoxes.Context(objectId))
    {
        public override int WiredCode => 0;

        protected override Task FillInternalDataAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class TestAddon(int objectId)
        : FurnitureWiredAddonLogic(Grains(), Stuff(), WiredTestBoxes.Context(objectId))
    {
        public override int WiredCode => 0;

        protected override Task FillInternalDataAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class TestAction(int objectId, bool hydrationThrows = false)
        : FurnitureWiredActionLogic(Grains(), Stuff(), WiredTestBoxes.Context(objectId))
    {
        public override int WiredCode => 0;

        protected override Task FillInternalDataAsync(CancellationToken ct) =>
            hydrationThrows
                ? throw new InvalidOperationException("this box will not load")
                : Task.CompletedTask;
    }

    private sealed class TestVariable(int objectId)
        : FurnitureWiredVariableLogic(Grains(), Stuff(), WiredTestBoxes.Context(objectId))
    {
        public override int WiredCode => 0;

        protected override WiredVariableTargetType TargetType => WiredVariableTargetType.Global;

        protected override WiredAvailabilityType AvailabilityType =>
            WiredAvailabilityType.RoomActive;

        protected override Task FillInternalDataAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
