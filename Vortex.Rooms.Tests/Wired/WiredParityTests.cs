using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Orleans;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Events.Player;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Grains.Systems;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;
using Vortex.Rooms.Wired.Engine;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The behavioural rules of the wired pipeline, driven end to end on a room that is not a grain.
/// </summary>
/// <remarks>
/// The architecture note asks for these by name (§6.4) and they were unwritable until the engine
/// stopped taking a <c>RoomGrain</c>. Each one is a rule that is invisible until it breaks: an effect
/// that fires twice, a delayed effect that fires from a tile it has left, a chain that runs at the
/// wrong logical time because the tick that carried it was late.
/// </remarks>
public sealed class WiredParityTests
{
    private const int TILE = 0;

    /// <summary>
    /// A zero-delay chain runs in the same drain as the trigger that scheduled it. Nothing waits for
    /// a second tick when nothing asked to wait.
    /// </summary>
    [Fact]
    public async Task AZeroDelayChainRunsInTheSameTick()
    {
        Harness h = new();
        h.WithTrigger().WithAction(delayMs: 0);

        await h.FireAsync(now: 1_000);

        h.Ran.Should().ContainSingle();
    }

    /// <summary>
    /// Every zero-delay action of a pile runs in that one drain, in pile order. A chain that stopped
    /// after the first would leave the rest of the pile silently unfired.
    /// </summary>
    [Fact]
    public async Task EveryZeroDelayActionOfThePileRunsInOrder()
    {
        Harness h = new();
        h.WithTrigger().WithAction(delayMs: 0, objectId: 30).WithAction(delayMs: 0, objectId: 20);

        await h.FireAsync(now: 1_000);

        h.Ran.Should().Equal([20, 30], "the pile resolves in object-id order");
    }

    /// <summary>
    /// The delay is measured on the room clock, not counted in ticks. An action delayed 500ms does
    /// not run on the tick that scheduled it, and does run on the first tick at or after its
    /// deadline.
    /// </summary>
    [Fact]
    public async Task ADelayedActionWaitsForItsDeadlineOnTheClock()
    {
        Harness h = new();
        h.WithTrigger().WithAction(delayMs: 500);

        await h.FireAsync(now: 1_000);

        h.Ran.Should().BeEmpty("scheduled, not run");

        await h.TickAsync(now: 1_400);

        h.Ran.Should().BeEmpty("still short of the deadline");

        await h.TickAsync(now: 1_500);

        h.Ran.Should().ContainSingle();
    }

    /// <summary>
    /// A tick that arrives late runs the action once, at the first tick past its deadline — the
    /// delay is not shortened, not stretched, and not applied twice to catch up. This is the rule a
    /// tick-counting delay gets wrong under load, which is exactly when it matters.
    /// </summary>
    [Fact]
    public async Task ALateTickRunsADelayedActionOnceAndOnlyOnce()
    {
        Harness h = new();
        h.WithTrigger().WithAction(delayMs: 500);

        await h.FireAsync(now: 1_000);

        // The room stalled: the next tick lands seconds after the deadline instead of 50ms after it.
        await h.TickAsync(now: 9_000);

        h.Ran.Should().ContainSingle("late is late, not late and repeated");

        await h.TickAsync(now: 9_050);
        await h.TickAsync(now: 20_000);

        h.Ran.Should().ContainSingle("and it stays run");
    }

    /// <summary>
    /// A delayed action dragged off the trigger's tile during its delay window does not fire. Habbo
    /// only lets a trigger drive the boxes stacked with it, and a delay is the one window in which
    /// that can stop being true after the pile was resolved.
    /// </summary>
    [Fact]
    public async Task ADelayedActionThatLeavesThePileDoesNotFire()
    {
        Harness h = new();
        h.WithTrigger().WithAction(delayMs: 500, objectId: 20);

        await h.FireAsync(now: 1_000);

        h.MoveOffTile(20);

        await h.TickAsync(now: 1_500);

        h.Ran.Should().BeEmpty("the action is no longer on the tile the trigger fired from");
    }

    /// <summary>
    /// The same action, still on its tile, does fire — otherwise the test above would pass for the
    /// wrong reason.
    /// </summary>
    [Fact]
    public async Task ADelayedActionThatStaysOnThePileFires()
    {
        Harness h = new();
        h.WithTrigger().WithAction(delayMs: 500, objectId: 20);

        await h.FireAsync(now: 1_000);
        await h.TickAsync(now: 1_500);

        h.Ran.Should().ContainSingle();
    }

    /// <summary>
    /// A delayed action picked up entirely during its delay window does not fire either. Gone from
    /// the room is a different check from moved off the tile, and both guard the same window.
    /// </summary>
    [Fact]
    public async Task ADelayedActionPickedUpDoesNotFire()
    {
        Harness h = new();
        h.WithTrigger().WithAction(delayMs: 500, objectId: 20);

        await h.FireAsync(now: 1_000);

        h.PickUp(20);

        await h.TickAsync(now: 1_500);

        h.Ran.Should().BeEmpty();
    }

    /// <summary>
    /// A trigger left in the index after its box is gone is skipped rather than fired, and the index
    /// is marked for rebuild so it corrects itself on the next tick.
    /// </summary>
    [Fact]
    public async Task AGhostTriggerIsSkippedAndTheIndexRepairsItself()
    {
        Harness h = new();
        h.WithTrigger().WithAction(delayMs: 0);

        // Index it, then take the box out of the room without touching the index.
        await h.TickAsync(now: 1_000);
        h.PickUp(Harness.TRIGGER_ID);

        await h.FireAsync(now: 1_100);

        h.Ran.Should().BeEmpty("the trigger is not in the room any more");

        // The next tick rebuilds, and the ghost is gone for good.
        await h.FireAsync(now: 1_200);

        h.Ran.Should().BeEmpty();
    }

    /// <summary>
    /// Past the queue cap the newcomer is refused, not an older event evicted. Rejecting the newest
    /// is what keeps the order of everything already accepted, which is the only reason a bounded
    /// queue is safe for triggers at all.
    /// </summary>
    [Fact]
    public async Task PastTheQueueCapTheNewcomerIsRefusedAndTheOrderHolds()
    {
        Harness h = new(maxQueuedEvents: 3);
        h.WithTrigger().WithAction(delayMs: 0);

        await h.TickAsync(now: 1_000);

        for (int i = 0; i < 5; i++)
        {
            await h.RaiseAsync();
        }

        h.Room.StopReasons.Should()
            .Equal(
                [WiredStopReason.QUEUE_DROP, WiredStopReason.QUEUE_DROP],
                "three fit and two were refused"
            );

        await h.TickAsync(now: 1_100);

        h.Ran.Should().HaveCount(3, "and the three that fit all fired");
    }

    private sealed class Harness
    {
        public const int TRIGGER_ID = 1;

        public FakeWiredRoomHost Room { get; }

        public List<int> Ran { get; } = [];

        private readonly RoomWiredSystem _engine;

        public Harness(int maxQueuedEvents = 512)
        {
            Room = new FakeWiredRoomHost { MaxQueuedEvents = maxQueuedEvents };
            _engine = new RoomWiredSystem(Room);
        }

        public Harness WithTrigger()
        {
            Room.With(WiredTestBoxes.FloorItem(TRIGGER_ID, new FiringTrigger(TRIGGER_ID)), TILE);

            return this;
        }

        public Harness WithAction(int delayMs, int objectId = 10)
        {
            Room.With(
                WiredTestBoxes.FloorItem(objectId, new RecordingAction(objectId, delayMs, Ran)),
                TILE
            );

            return this;
        }

        /// <summary>Raises an event and ticks, so the pile fires within this call.</summary>
        public async Task FireAsync(long now)
        {
            await RaiseAsync();
            await TickAsync(now);
        }

        public Task RaiseAsync() =>
            _engine.OnRoomEventAsync(
                new PlayerLeftEvent
                {
                    RoomId = Room.RoomId,
                    CausedBy = ActionContext.CreateForWired(Room.RoomId),
                    PlayerId = new PlayerId(1),
                },
                CancellationToken.None
            );

        public Task TickAsync(long now) => _engine.ProcessWiredAsync(now, CancellationToken.None);

        /// <summary>Drags a box onto another tile, leaving it in the room.</summary>
        public void MoveOffTile(int objectId) => Room.MoveToTile(objectId, TILE + 1);

        /// <summary>Takes a box out of the room entirely.</summary>
        public void PickUp(int objectId) => Room.RemoveCompletely(objectId);
    }

    /// <summary>A trigger that listens for the event the harness raises, and always fires.</summary>
    private sealed class FiringTrigger(int objectId)
        : FurnitureWiredTriggerLogic(
            FakeProxy.Create<IGrainFactory>(_ => null),
            new StuffDataFactory(),
            WiredTestBoxes.Context(objectId, TILE)
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

    /// <summary>An action that records that it ran, and waits as long as it is told to.</summary>
    private sealed class RecordingAction(int objectId, int delayMs, List<int> ran)
        : FurnitureWiredActionLogic(
            FakeProxy.Create<IGrainFactory>(_ => null),
            new StuffDataFactory(),
            WiredTestBoxes.Context(objectId, TILE)
        )
    {
        public override int WiredCode => 0;

        public override int GetDelayMs() => delayMs;

        public override Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
        {
            ran.Add(objectId);

            return Task.FromResult(true);
        }

        protected override Task FillInternalDataAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
