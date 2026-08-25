using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Events.Player;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;
using Vortex.Rooms.Wired.Engine;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The registry of which trigger boxes listen for what — run against a fake room, with no grain.
/// </summary>
/// <remarks>
/// None of this had a test before. The rules it encodes are the ones that decide whether a room's
/// wired does anything at all: a trigger that will not hydrate must not take the rebuild down with
/// it, and a room with no triggers has to be recognisable so queued events can be dropped instead of
/// accumulating.
/// </remarks>
public sealed class WiredTriggerIndexTests
{
    [Fact]
    public async Task ARebuildIndexesEachTriggerUnderEveryEventTypeItListensFor()
    {
        FakeWiredRoomHost room = new();
        room.With(Trigger(1, [typeof(PlayerEnterEvent), typeof(PlayerLeftEvent)]));
        room.With(Trigger(2, [typeof(PlayerLeftEvent)]));

        WiredTriggerIndex index = Build(room);
        await index.RebuildAsync(CancellationToken.None);

        index.Listens(typeof(PlayerEnterEvent)).Should().BeTrue();
        index.Listening(typeof(PlayerEnterEvent)).Should().ContainSingle();
        index.Listening(typeof(PlayerLeftEvent)).Should().HaveCount(2);
    }

    [Fact]
    public async Task AnEventNobodyListensFor_IsNotIndexed()
    {
        FakeWiredRoomHost room = new();
        room.With(Trigger(1, [typeof(PlayerLeftEvent)]));

        WiredTriggerIndex index = Build(room);
        await index.RebuildAsync(CancellationToken.None);

        index.Listens(typeof(PlayerEnterEvent)).Should().BeFalse();
        index.Listening(typeof(PlayerEnterEvent)).Should().BeEmpty();
    }

    /// <summary>
    /// A trigger that will not hydrate is skipped. It used to be the only failure mode here, and
    /// letting it escape would cost the room every trigger indexed after it — the room's wired would
    /// go quiet with one warning to explain it.
    /// </summary>
    [Fact]
    public async Task ATriggerThatFailsToHydrate_IsSkippedAndTheRestSurvive()
    {
        FakeWiredRoomHost room = new();
        room.With(Trigger(1, [typeof(PlayerLeftEvent)], hydrationThrows: true));
        room.With(Trigger(2, [typeof(PlayerLeftEvent)]));

        WiredTriggerIndex index = Build(room);
        await index.RebuildAsync(CancellationToken.None);

        index
            .Listening(typeof(PlayerLeftEvent))
            .Should()
            .ContainSingle("the one that hydrated is still indexed")
            .Which.ObjectId.Value.Should()
            .Be(2);
    }

    [Fact]
    public async Task ATimedTrigger_LandsInTheTimedListAsWellAsItsEventTypes()
    {
        FakeWiredRoomHost room = new();
        room.With(TimedTrigger(1));
        room.With(Trigger(2, [typeof(PlayerLeftEvent)]));

        WiredTriggerIndex index = Build(room);
        await index.RebuildAsync(CancellationToken.None);

        index.Timed.Should().ContainSingle().Which.ObjectId.Value.Should().Be(1);
    }

    /// <summary>
    /// An index that has never been built is dirty, not empty. The two are different: an empty room
    /// legitimately has no triggers, and treating "not built yet" as "nothing to do" would mean a
    /// room's wired never started.
    /// </summary>
    [Fact]
    public void ANewIndex_IsDirty()
    {
        Build(new FakeWiredRoomHost()).IsDirty.Should().BeTrue();
    }

    [Fact]
    public async Task ARebuild_ClearsTheDirtyFlag()
    {
        WiredTriggerIndex index = Build(new FakeWiredRoomHost());

        await index.RebuildAsync(CancellationToken.None);

        index.IsDirty.Should().BeFalse();

        index.MarkDirty();

        index.IsDirty.Should().BeTrue();
    }

    /// <summary>
    /// A room with no triggers of any kind is recognisable, so the caller can drop queued events
    /// instead of letting the queue grow for nothing to consume.
    /// </summary>
    [Fact]
    public async Task ARoomWithNoTriggers_IsEmpty()
    {
        WiredTriggerIndex index = Build(new FakeWiredRoomHost());

        await index.RebuildAsync(CancellationToken.None);

        index.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task ARebuild_ForgetsTheTriggersThatAreGone()
    {
        FakeWiredRoomHost room = new();
        room.With(Trigger(1, [typeof(PlayerLeftEvent)]));

        WiredTriggerIndex index = Build(room);
        await index.RebuildAsync(CancellationToken.None);

        room.RemoveItemOnly(new RoomObjectId(1));
        await index.RebuildAsync(CancellationToken.None);

        index.IsEmpty.Should().BeTrue();
    }

    /// <summary>
    /// The listening list is a snapshot. Firing an action can add or remove room furniture, and a
    /// caller iterating the live list would be iterating something its own loop is changing.
    /// </summary>
    [Fact]
    public async Task TheListeningList_IsASnapshotOfItsOwn()
    {
        FakeWiredRoomHost room = new();
        room.With(Trigger(1, [typeof(PlayerLeftEvent)]));

        WiredTriggerIndex index = Build(room);
        await index.RebuildAsync(CancellationToken.None);

        IReadOnlyList<FurnitureWiredTriggerLogic> first = index.Listening(typeof(PlayerLeftEvent));

        await index.RebuildAsync(CancellationToken.None);

        first.Should().ContainSingle("the list handed out earlier is not the one that was rebuilt");
    }

    private static WiredTriggerIndex Build(FakeWiredRoomHost room) =>
        new(room.View, room.Diagnostics);

    private static IRoomFloorItem Trigger(
        int objectId,
        List<Type> eventTypes,
        bool hydrationThrows = false
    ) => WiredTestBoxes.FloorItem(objectId, new TestTrigger(objectId, eventTypes, hydrationThrows));

    private static IRoomFloorItem TimedTrigger(int objectId) =>
        WiredTestBoxes.FloorItem(objectId, new TestTimedTrigger(objectId));

    private class TestTrigger(int objectId, List<Type> eventTypes, bool hydrationThrows)
        : FurnitureWiredTriggerLogic(
            FakeProxy.Create<IGrainFactory>(_ => null),
            FakeProxy.Create<IStuffDataFactory>(_ => null),
            WiredTestBoxes.Context(objectId)
        )
    {
        /// <summary>The client's box id. Nothing in the index reads it; the base class requires one.</summary>
        public override int WiredCode => 0;

        public override List<Type> SupportedEventTypes { get; } = eventTypes;

        protected override Task FillInternalDataAsync(CancellationToken ct) =>
            hydrationThrows
                ? throw new InvalidOperationException("this box will not load")
                : Task.CompletedTask;
    }

    private sealed class TestTimedTrigger : TestTrigger, IWiredTimedTrigger
    {
        public TestTimedTrigger(int objectId)
            : base(objectId, [], hydrationThrows: false) { }

        public bool TryConsumeDue(long nowMs) => false;
    }
}
