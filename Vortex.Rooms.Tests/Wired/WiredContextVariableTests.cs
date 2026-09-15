using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Orleans;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Action;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Events.Player;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains.Systems;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Variables;
using Vortex.Rooms.Wired;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The context variable box (<c>wf_var_context</c>): a value that describes the chain running right
/// now and does not outlive it.
/// </summary>
/// <remarks>
/// It was a dead box. Its keys are <see cref="WiredVariableTargetType.Context"/>, and the room's
/// store lookup knew only Furni, User and Global and threw on everything else — so every read and
/// every write of it raised, was swallowed by the engine's per-action catch, and the builder saw a
/// box that did nothing. Context is a destination the change-variable box explicitly offers, so it
/// was reachable, not theoretical.
/// </remarks>
public sealed class WiredContextVariableTests
{
    private const int Tile = 0;

    private const int BoxId = 5;

    [Fact]
    public void ReadOutsideAnyChain_IsAMissRatherThanAThrow()
    {
        Harness harness = new();

        Func<bool> read = () => harness.Box.TryGetValue(harness.Key, out _);

        read.Should().NotThrow("nothing is running, so the box describes no chain");
        read().Should().BeFalse();
    }

    [Fact]
    public async Task AValueWrittenByOneEffectIsThereForTheNext()
    {
        Harness harness = new();

        await harness.FireAsync();

        harness.Writer.Wrote.Should().BeTrue();
        harness.Reader.Read.Should().Be(7, "the chain is the scope, not the effect");
    }

    /// <summary>
    /// The next firing starts empty. A context variable that survived its chain would hand the next
    /// run the previous one's numbers, which is the bug the ambient's lifetime rules exist to stop.
    /// </summary>
    [Fact]
    public async Task ThenextFiringStartsFromNothing()
    {
        Harness harness = new();

        await harness.FireAsync();

        harness.Writer.Skip = true;
        harness.Reader.Read = -1;

        await harness.FireAsync(2_000);

        harness.Reader.Read.Should().Be(0, "a fresh chain holds nothing this one did not write");
    }

    private sealed class Harness
    {
        private readonly FakeWiredRoomHost _room = new();

        private readonly RoomWiredSystem _engine;

        public TestContextVariable Box { get; }

        public WriterAction Writer { get; }

        public ReaderAction Reader { get; }

        public WiredVariableKey Key =>
            new(Box.GetVarSnapshot().VariableId, WiredVariableTargetType.Context, 0);

        public Harness()
        {
            FakeFurniAccess furni = new();

            Box = new TestContextVariable(
                WiredTestBoxes.Context(BoxId, furniAccess: furni),
                new WiredData { StringParam = "round_score", IntParams = [1] }
            );

            _room.With(WiredTestBoxes.FloorItem(1, new PlayerLeftTrigger()), Tile);

            Writer = new WriterAction(2, Box, () => Key);
            Reader = new ReaderAction(3, Box, () => Key);

            _room.With(WiredTestBoxes.FloorItem(2, Writer), Tile);
            _room.With(WiredTestBoxes.FloorItem(3, Reader), Tile);

            _engine = new RoomWiredSystem(_room);

            RoomWiredSystem engine = _engine;

            // The room decides at read time whether a context key has a store: one exists only
            // while a chain is running, which is the whole behaviour under test.
            furni.VariableStoreFor = key =>
                engine.TryGetStoreForKey(
                    key,
                    out global::Vortex.Rooms.Grains.Storage.KeyValueStore? store
                )
                    ? store
                    : null;
        }

        public async Task FireAsync(long now = 1_000)
        {
            await _engine.OnRoomEventAsync(
                new PlayerLeftEvent
                {
                    RoomId = _room.RoomId,
                    CausedBy = ActionContext.CreateForWired(_room.RoomId),
                    PlayerId = new Primitives.Players.PlayerId(1),
                },
                CancellationToken.None
            );

            await _engine.ProcessWiredAsync(now, CancellationToken.None);
        }
    }

    private sealed class TestContextVariable : WiredVariableContext
    {
        public TestContextVariable(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx)
        {
            data.AttatchRules(GetIntParamRules());
            _wiredData = data;
        }
    }

    /// <summary>Writes 7 into the context variable, the way the change-variable box would.</summary>
    private sealed class WriterAction(
        int objectId,
        TestContextVariable box,
        Func<WiredVariableKey> key
    )
        : FurnitureWiredActionLogic(
            FakeProxy.Create<IGrainFactory>(_ => null),
            new StuffDataFactory(),
            WiredTestBoxes.Context(objectId)
        )
    {
        public bool Wrote { get; private set; }

        public bool Skip { get; set; }

        public override int WiredCode => 0;

        public override async Task<bool> ExecuteAsync(
            IWiredExecutionContext ctx,
            CancellationToken ct
        )
        {
            if (!Skip)
            {
                Wrote = await box.GiveValueAsync(key(), new WiredVariableValue(7), replace: true);
            }

            return true;
        }

        protected override Task FillInternalDataAsync(CancellationToken ct) => Task.CompletedTask;
    }

    /// <summary>Reads it back, from a later effect in the same chain.</summary>
    private sealed class ReaderAction(
        int objectId,
        TestContextVariable box,
        Func<WiredVariableKey> key
    )
        : FurnitureWiredActionLogic(
            FakeProxy.Create<IGrainFactory>(_ => null),
            new StuffDataFactory(),
            WiredTestBoxes.Context(objectId)
        )
    {
        public int Read { get; set; } = -1;

        public override int WiredCode => 0;

        public override Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
        {
            Read = box.TryGetValue(key(), out WiredVariableValue value) ? value.Value : 0;

            return Task.FromResult(true);
        }

        protected override Task FillInternalDataAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class PlayerLeftTrigger()
        : FurnitureWiredTriggerLogic(
            FakeProxy.Create<IGrainFactory>(_ => null),
            new StuffDataFactory(),
            WiredTestBoxes.Context(1)
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
}
