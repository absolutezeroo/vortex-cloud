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
using Vortex.Rooms.Grains.Systems;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;
using Vortex.Rooms.Wired.Engine;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The whole wired system, constructed on a room that is not a grain.
/// </summary>
/// <remarks>
/// This is what the extraction was for. <c>RoomWiredSystem</c> used to take a <c>RoomGrain</c> and
/// reach into its fields, so running the pipeline meant building most of a room — the leaves had 33
/// files of tests and the orchestrator had none. It takes an <c>IWiredRoomHost</c> now, and this is
/// the proof: a system that builds, ticks and drains queued events with nothing behind it.
/// <para>
/// The behavioural parity matrix the architecture note asks for is built on this seam. What is here
/// is the seam itself working end to end.
/// </para>
/// </remarks>
public sealed class WiredEngineOnAFakeRoomTests
{
    [Fact]
    public void TheEngineBuildsWithoutAGrain()
    {
        FakeWiredRoomHost room = new();

        Action build = () => _ = new RoomWiredSystem(room);

        build.Should().NotThrow();
    }

    /// <summary>
    /// An empty room ticks and does nothing. Worth its own test because "does nothing" used to be
    /// indistinguishable from "could not run at all".
    /// </summary>
    [Fact]
    public async Task AnEmptyRoomTicksQuietly()
    {
        FakeWiredRoomHost room = new();
        RoomWiredSystem engine = new(room);

        await engine.ProcessWiredAsync(1_000, CancellationToken.None);

        room.StopReasons.Should().BeEmpty();
        room.RoomLog.Should().BeEmpty();
    }

    /// <summary>
    /// A room with no triggers cannot consume anything, so queued events are dropped rather than
    /// left to grow. The queue is bounded too, but a bound nothing ever drains is still a leak.
    /// </summary>
    [Fact]
    public async Task ARoomWithNoTriggersDropsWhatItCannotConsume()
    {
        FakeWiredRoomHost room = new();
        RoomWiredSystem engine = new(room);

        for (int i = 0; i < 50; i++)
        {
            await engine.OnRoomEventAsync(PlayerLeft(room), CancellationToken.None);
        }

        await engine.ProcessWiredAsync(1_000, CancellationToken.None);

        room.StopReasons.Should()
            .BeEmpty("dropping events nothing listens for is routine, not a chain stopping");
    }

    /// <summary>
    /// The queue is bounded: past WiredMaxQueuedEvents an incoming event is refused, and refusing it
    /// is counted. Rejecting the newcomer rather than evicting an older one is what keeps trigger
    /// ordering intact for everything already accepted.
    /// </summary>
    [Fact]
    public async Task AnEventStormPastTheQueueCapIsCountedAsDropped()
    {
        FakeWiredRoomHost room = new() { MaxQueuedEvents = 4 };
        room.With(Trigger(1, [typeof(PlayerLeftEvent)]), tileIdx: 0);

        RoomWiredSystem engine = new(room);

        // The first tick builds the index, so the engine knows something listens for this event.
        await engine.ProcessWiredAsync(1_000, CancellationToken.None);

        for (int i = 0; i < 10; i++)
        {
            await engine.OnRoomEventAsync(PlayerLeft(room), CancellationToken.None);
        }

        room.StopReasons.Should()
            .OnlyContain(reason => reason == Primitives.Observability.WiredStopReason.QUEUE_DROP)
            .And.HaveCount(6, "four fit, six did not");
    }

    /// <summary>
    /// A tick with a trigger in the room hydrates it and leaves the index clean — the orchestrator's
    /// first real step, and one that could not be observed before without a room.
    /// </summary>
    [Fact]
    public async Task ATickIndexesTheTriggersItFinds()
    {
        FakeWiredRoomHost room = new();
        room.With(Trigger(1, [typeof(PlayerLeftEvent)]), tileIdx: 0);

        RoomWiredSystem engine = new(room);

        await engine.ProcessWiredAsync(1_000, CancellationToken.None);

        // Nothing fired — the trigger's pile has no actions — but nothing refused anything either.
        room.StopReasons.Should().BeEmpty();
    }

    private static PlayerLeftEvent PlayerLeft(FakeWiredRoomHost room) =>
        new()
        {
            RoomId = room.RoomId,
            CausedBy = ActionContext.CreateForWired(room.RoomId),
            PlayerId = new PlayerId(1),
        };

    private static IRoomFloorItem Trigger(int objectId, List<Type> eventTypes) =>
        WiredTestBoxes.FloorItem(objectId, new TestTrigger(objectId, eventTypes));

    private sealed class TestTrigger(int objectId, List<Type> eventTypes)
        : FurnitureWiredTriggerLogic(
            FakeProxy.Create<IGrainFactory>(_ => null),
            new StuffDataFactory(),
            WiredTestBoxes.Context(objectId)
        )
    {
        public override int WiredCode => 0;

        public override List<Type> SupportedEventTypes { get; } = eventTypes;

        protected override Task FillInternalDataAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
