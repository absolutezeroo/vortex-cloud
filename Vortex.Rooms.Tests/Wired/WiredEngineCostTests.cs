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
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Events.Player;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Rooms.Grains.Systems;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;
using Vortex.Rooms.Wired.Engine;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// What a wired tick costs, counted rather than timed.
/// </summary>
/// <remarks>
/// <para>
/// The architecture note asks for a versioned benchmark of the empty room, the loaded room, an event
/// storm and "no-trigger ≈ O(1)", with a baseline to refuse regressions against. Three of those four
/// are wall-clock questions and belong in
/// <c>docs/architecture-v4/benchmarks/</c>, where a number measured on one machine can at least be
/// compared against the same machine later.
/// </para>
/// <para>
/// The fourth is not a wall-clock question at all. "O(1) in the size of the room" is a claim about
/// which calls the engine makes, and the fake host counts them exactly: <c>AllItems</c>,
/// <c>AllItemIds</c> and <c>AllAvatarPlayerIds</c> are the only members whose cost grows with the
/// room, so a per-tick scan count that does not move when the room grows from ten items to a thousand
/// is the claim, proved, on any machine and in any mood. A timing test for the same thing would be
/// noise on a laptop and a flake in CI.
/// </para>
/// </remarks>
public sealed class WiredEngineCostTests
{
    /// <summary>
    /// The claim itself. Two rooms, a hundred times the furniture, and the same steady-state cost —
    /// which only holds while the trigger index stays clean and nothing walks the room per tick.
    /// </summary>
    [Theory]
    [InlineData(10)]
    [InlineData(1_000)]
    public async Task ASteadyTickCostsTheSameWhateverTheRoomHolds(int items)
    {
        (FakeWiredRoomHost room, RoomWiredSystem engine) = Room(items, withTrigger: false);

        // First tick builds the index; that one scan is the price of admission and is paid once.
        await engine.ProcessWiredAsync(1_000, CancellationToken.None);

        room.ResetScans();

        for (int tick = 1; tick <= 20; tick++)
        {
            await engine.ProcessWiredAsync(1_000 + (tick * 50), CancellationToken.None);
        }

        room.Scans.Should().Be(0, "a clean index is not rebuilt, and nothing else walks the room");
    }

    /// <summary>
    /// The index is rebuilt once, not once per item. Stated separately because the test above would
    /// also pass if the first tick scanned a thousand times.
    /// </summary>
    [Fact]
    public async Task TheOneScanIsTheIndexRebuildAndThereIsOnlyOne()
    {
        (FakeWiredRoomHost room, RoomWiredSystem engine) = Room(1_000, withTrigger: false);

        await engine.ProcessWiredAsync(1_000, CancellationToken.None);

        room.Scans.Should().Be(1);
        room.IndexRebuilds.Should().Be(1);
    }

    /// <summary>
    /// An event storm against a room nothing listens in costs no scans at all. This is the shape that
    /// matters under load: a busy room full of players raises thousands of events a minute, and a room
    /// with no wired must not pay for them.
    /// </summary>
    [Fact]
    public async Task AnEventStormAgainstARoomWithNoWiredCostsNothingPerEvent()
    {
        (FakeWiredRoomHost room, RoomWiredSystem engine) = Room(500, withTrigger: false);

        await engine.ProcessWiredAsync(1_000, CancellationToken.None);

        room.ResetScans();

        for (int i = 0; i < 2_000; i++)
        {
            await engine.OnRoomEventAsync(PlayerLeft(room), CancellationToken.None);
        }

        await engine.ProcessWiredAsync(1_050, CancellationToken.None);

        room.Scans.Should().Be(0);
        room.EventOutcomes.Should().HaveCount(2_000).And.OnlyContain(o => o == "ignored");
    }

    /// <summary>
    /// A wave of wired boxes being rearranged costs exactly one rebuild on the next tick — not one per
    /// box, and not one per tick until the room settles. This is a builder dragging a pile around,
    /// which is when the index is dirtied hardest and when a per-change rebuild would hurt most.
    /// </summary>
    [Fact]
    public async Task AWaveOfStackChangesCostsOneRebuild()
    {
        (FakeWiredRoomHost room, RoomWiredSystem engine) = Room(200, withTrigger: true);

        await engine.ProcessWiredAsync(1_000, CancellationToken.None);

        room.ResetScans();

        for (int objectId = 1; objectId <= 100; objectId++)
        {
            await engine.OnRoomEventAsync(StackChanged(room, objectId), CancellationToken.None);
        }

        room.Scans.Should().Be(0, "a stack change flags the index; it does not rebuild it");

        await engine.ProcessWiredAsync(1_050, CancellationToken.None);
        await engine.ProcessWiredAsync(1_100, CancellationToken.None);

        room.Scans.Should().Be(1, "one rebuild covers the whole wave, and the next tick is clean");
    }

    private static (FakeWiredRoomHost Room, RoomWiredSystem Engine) Room(
        int items,
        bool withTrigger
    )
    {
        FakeWiredRoomHost room = new();

        for (int objectId = 1; objectId <= items; objectId++)
        {
            room.With(WiredTestBoxes.FloorItem(objectId, logic: null!), tileIdx: objectId % 50);
        }

        if (withTrigger)
        {
            room.With(WiredTestBoxes.FloorItem(items + 1, new IdleTrigger(items + 1)), tileIdx: 99);
        }

        return (room, new RoomWiredSystem(room));
    }

    private static RoomWiredStackChangedEvent StackChanged(FakeWiredRoomHost room, int stackId) =>
        new()
        {
            RoomId = room.RoomId,
            CausedBy = ActionContext.CreateForWired(room.RoomId),
            StackIds = [stackId],
        };

    private static PlayerLeftEvent PlayerLeft(FakeWiredRoomHost room) =>
        new()
        {
            RoomId = room.RoomId,
            CausedBy = ActionContext.CreateForWired(room.RoomId),
            PlayerId = new PlayerId(1),
        };

    /// <summary>A trigger that listens for nothing, so the index is populated but nothing ever fires.</summary>
    private sealed class IdleTrigger(int objectId)
        : FurnitureWiredTriggerLogic(
            FakeProxy.Create<IGrainFactory>(_ => null),
            new StuffDataFactory(),
            WiredTestBoxes.Context(objectId, 99)
        )
    {
        public override int WiredCode => 0;

        public override List<Type> SupportedEventTypes { get; } = [];

        protected override Task FillInternalDataAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
