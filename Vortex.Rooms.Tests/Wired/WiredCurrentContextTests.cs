using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Orleans;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Events.Player;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Grains.Systems;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The chain being executed, published where a context variable can read it.
/// </summary>
/// <remarks>
/// <c>@selector_furni_count</c>, <c>@signal_user_count</c> and the rest of that tab describe the
/// run, not the room. They are read through <c>IWiredVariable.TryGetValue</c>, which is handed a key
/// and nothing else, and the execution context was built per action and thrown away — so the whole
/// tab could only answer zero, and its one implementation shipped with the value line commented out.
/// <para>
/// What is worth guarding is not the arithmetic of the counters but the LIFETIME of that ambient
/// field: visible while the action runs, gone afterwards, and gone even when the action throws.
/// A stale context would make the next room's readings answer with a previous chain's numbers.
/// </para>
/// </remarks>
public sealed class WiredCurrentContextTests
{
    private const int Tile = 0;

    [Fact]
    public async Task TheRunningChainIsReadableWhileItRuns_AndGoneOnceItHas()
    {
        FakeWiredRoomHost room = new();
        ObservingAction action = new(2);

        room.With(Trigger(1), Tile);
        room.With(WiredTestBoxes.FloorItem(2, action), Tile);

        RoomWiredSystem engine = new(room);

        action.Engine = engine;

        await engine.OnRoomEventAsync(PlayerLeft(room), CancellationToken.None);
        await engine.ProcessWiredAsync(1_000, CancellationToken.None);

        action.Ran.Should().BeTrue("the pile has a trigger and an action on one tile");
        action.SawContext.Should().BeTrue("a context variable read during the action must find it");
        engine.CurrentContext.Should().BeNull("and must not find one after the chain is over");
    }

    [Fact]
    public async Task AnActionThatThrowsDoesNotLeaveItsContextStanding()
    {
        FakeWiredRoomHost room = new();
        ThrowingAction action = new(2);

        room.With(Trigger(1), Tile);
        room.With(WiredTestBoxes.FloorItem(2, action), Tile);

        RoomWiredSystem engine = new(room);

        await engine.OnRoomEventAsync(PlayerLeft(room), CancellationToken.None);
        await engine.ProcessWiredAsync(1_000, CancellationToken.None);

        action.Ran.Should().BeTrue("otherwise this test proves nothing about the failure path");
        engine
            .CurrentContext.Should()
            .BeNull("the engine swallows the failure, so only the finally can clear it");
    }

    private static PlayerLeftEvent PlayerLeft(FakeWiredRoomHost room) =>
        new()
        {
            RoomId = room.RoomId,
            CausedBy = ActionContext.CreateForWired(room.RoomId),
            PlayerId = new PlayerId(1),
        };

    private static IRoomFloorItem Trigger(int objectId) =>
        WiredTestBoxes.FloorItem(objectId, new FiringTrigger(objectId));

    /// <summary>Listens for the event these tests raise, and always fires.</summary>
    private sealed class FiringTrigger(int objectId)
        : FurnitureWiredTriggerLogic(
            FakeProxy.Create<IGrainFactory>(_ => null),
            new StuffDataFactory(),
            WiredTestBoxes.Context(objectId, Tile)
        )
    {
        public override int WiredCode => 0;

        public override List<Type> SupportedEventTypes { get; } = [typeof(PlayerLeftEvent)];

        public override Task<bool> CanTriggerAsync(
            IWiredProcessingContext ctx,
            CancellationToken ct
        ) => Task.FromResult(true);

        protected override Task FillInternalDataAsync(CancellationToken ct) => Task.CompletedTask;
    }

    /// <summary>Stands in for a context variable: reads the ambient field from inside the run.</summary>
    private sealed class ObservingAction(int objectId)
        : FurnitureWiredActionLogic(
            FakeProxy.Create<IGrainFactory>(_ => null),
            new StuffDataFactory(),
            WiredTestBoxes.Context(objectId, Tile)
        )
    {
        public RoomWiredSystem? Engine { get; set; }

        public bool Ran { get; private set; }

        public bool SawContext { get; private set; }

        public override int WiredCode => 0;

        public override int GetDelayMs() => 0;

        protected override Task FillInternalDataAsync(CancellationToken ct) => Task.CompletedTask;

        public override Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
        {
            Ran = true;
            SawContext = Engine?.CurrentContext is not null;

            return Task.FromResult(true);
        }
    }

    private sealed class ThrowingAction(int objectId)
        : FurnitureWiredActionLogic(
            FakeProxy.Create<IGrainFactory>(_ => null),
            new StuffDataFactory(),
            WiredTestBoxes.Context(objectId, Tile)
        )
    {
        public bool Ran { get; private set; }

        public override int WiredCode => 0;

        public override int GetDelayMs() => 0;

        protected override Task FillInternalDataAsync(CancellationToken ct) => Task.CompletedTask;

        public override Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
        {
            Ran = true;

            throw new InvalidOperationException("the action failed");
        }
    }
}
