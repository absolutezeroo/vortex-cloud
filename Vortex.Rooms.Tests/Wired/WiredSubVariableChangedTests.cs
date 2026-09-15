using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains.Storage;
using Vortex.Rooms.Grains.Systems;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Variables;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Variables;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// A level-up add-on's readings are variables in their own right — the picker offers them, and the
/// "variable changed" trigger's whole reason to exist is "fire when the level goes up". The write
/// lands on the parent, so the room has to say that every reading of that parent moved too, or a
/// trigger watching one waits forever.
/// </summary>
public sealed class WiredSubVariableChangedTests
{
    private const int BoxId = 7;

    private const int AddonId = 8;

    private const int Player = 42;

    /// <summary>Slot 0 of <see cref="WiredAddonVariableLevelUp.Readings"/>.</summary>
    private const int CurrentLevelSlot = 0;

    /// <summary>Slot 1, the reading the change-variable box writes to.</summary>
    private const int CurrentXpSlot = 1;

    [Fact]
    public async Task WritingTheExperienceFiresATriggerWatchingTheLevel()
    {
        Harness harness = new();

        await harness.StartAsync();
        await harness.SeedExperienceAsync(25);

        // "Add 25" four times over, as the change-variable box does: the write is handed to the
        // current_xp reading, which runs itself backwards onto the parent.
        await harness.WriteAsync(CurrentXpSlot, 125);
        await harness.TickAsync();

        WiredVariableChangedEvent seen = harness
            .Offered.Should()
            .ContainSingle(evt => evt.Key.VariableId == harness.SubId(CurrentLevelSlot))
            .Subject;

        seen.Kind.Should().Be(WiredVariableChangeKind.ValueChanged);
        seen.Previous.Should().Be(1, "25 experience is level 1");
        seen.Current.Should().Be(2, "125 experience is level 2");
    }

    /// <summary>
    /// The reading the write was aimed at moves too. It reaches the trigger by the same route as any
    /// other reading — the parent's change — so leaving it out would make the one variable the
    /// builder actually picked the one that could not be watched.
    /// </summary>
    [Fact]
    public async Task TheReadingThatWasWrittenIsAnnouncedUnderItsOwnId()
    {
        Harness harness = new();

        await harness.StartAsync();
        await harness.SeedExperienceAsync(25);
        await harness.WriteAsync(CurrentXpSlot, 125);
        await harness.TickAsync();

        harness
            .Offered.Should()
            .ContainSingle(evt =>
                evt.Key.VariableId == harness.SubId(CurrentXpSlot)
                && evt.Previous == 25
                && evt.Current == 125
            );
    }

    /// <summary>The parent keeps announcing itself: a trigger on <c>user_data</c> watches the
    /// experience, and it was watching it before any reading existed.</summary>
    [Fact]
    public async Task TheParentIsStillAnnouncedInItsOwnRight()
    {
        Harness harness = new();

        await harness.StartAsync();
        await harness.SeedExperienceAsync(25);
        await harness.WriteAsync(CurrentXpSlot, 125);
        await harness.TickAsync();

        harness
            .Offered.Should()
            .ContainSingle(evt => evt.Key.VariableId == harness.ParentId && evt.Current == 125);
    }

    private sealed class Harness
    {
        private readonly FakeWiredRoomHost _room = new();

        private readonly RoomWiredSystem _engine;

        private readonly TestUserVariable _variable;

        /// <summary>Every change the engine handed the trigger, in order.</summary>
        public List<WiredVariableChangedEvent> Offered { get; } = [];

        /// <summary>What the box published and the room has not been handed yet. A room grain
        /// forwards these itself; here they are held so the test can stay asynchronous.</summary>
        private readonly List<RoomEvent> _published = [];

        public WiredVariableId ParentId => _variable.GetVarSnapshot().VariableId;

        public WiredVariableId SubId(int slot) =>
            WiredVariableIdBuilder.CreateFromBoxSubId(AddonId, slot);

        public Harness()
        {
            FakeFurniAccess furni = new() { VariableStore = new KeyValueStore() };

            _variable = new TestUserVariable(
                WiredTestBoxes.Context(BoxId, furniAccess: furni, published: _published.Add),
                new WiredData
                {
                    StringParam = "user_data",
                    IntParams = [(int)WiredAvailabilityType.UserActive, 1],
                }
            );

            // Mask 0b11: current_level and current_xp. Linear curve, 100 experience a level, 59
            // levels -- the shape in the report.
            TestLevelUp addon = new(
                WiredTestBoxes.Context(AddonId),
                new WiredData { IntParams = [0b11, 1, 100, 59] }
            );

            _room.With(Floor(BoxId, _variable), tileIdx: 0).With(Floor(AddonId, addon), tileIdx: 0);
            _room.With(Floor(99, new RecordingTrigger(Offered)), tileIdx: 1);

            _engine = new RoomWiredSystem(_room);
        }

        /// <summary>Builds the trigger index and registers the box's readings.</summary>
        public async Task StartAsync()
        {
            await _engine.OnRoomEventAsync(
                new WiredVariableBoxChangedEvent
                {
                    RoomId = _room.RoomId,
                    CausedBy = Primitives.Action.ActionContext.CreateForWired(_room.RoomId),
                    BoxIds = [BoxId],
                },
                CancellationToken.None
            );

            await _engine.ProcessWiredAsync(1_000, CancellationToken.None);
        }

        /// <summary>The experience the player already had. Announced like any other write, so the
        /// tick that follows drains it and leaves the assertions to the write under test.</summary>
        public async Task SeedExperienceAsync(int experience)
        {
            await _variable.GiveValueAsync(Key(ParentId), new WiredVariableValue(experience));

            await DrainAsync();
            await _engine.ProcessWiredAsync(1_100, CancellationToken.None);

            Offered.Clear();
        }

        /// <summary>Writes through one of the add-on's readings, the way the change-variable box
        /// does once it has resolved the variable the builder picked.</summary>
        public Task WriteAsync(int slot, int value)
        {
            IWiredVariable reading =
                _engine.GetVariableById(SubId(slot))
                ?? throw new Xunit.Sdk.XunitException($"Reading {slot} was never registered.");

            return reading.SetValueAsync(null!, Key(SubId(slot)), new WiredVariableValue(value));
        }

        public async Task TickAsync()
        {
            await DrainAsync();
            await _engine.ProcessWiredAsync(1_200, CancellationToken.None);
        }

        /// <summary>Hands the room what the boxes published, the way the grain does.</summary>
        private async Task DrainAsync()
        {
            List<RoomEvent> pending = [.. _published];
            _published.Clear();

            foreach (RoomEvent evt in pending)
            {
                await _engine.OnRoomEventAsync(evt, CancellationToken.None);
            }
        }

        private static WiredVariableKey Key(WiredVariableId id) =>
            new(id, WiredVariableTargetType.User, Player);

        private static IRoomFloorItem Floor(int objectId, object logic)
        {
            RoomObjectId id = new(objectId);

            return FakeProxy.Create<IRoomFloorItem>(call =>
                call.Method.Name switch
                {
                    "get_ObjectId" => id,
                    "get_Logic" => logic,
                    "get_X" => 0,
                    "get_Y" => 0,
                    _ => null,
                }
            );
        }
    }

    private sealed class TestUserVariable : WiredVariableUser
    {
        public TestUserVariable(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx)
        {
            data.AttatchRules(GetIntParamRules());
            _wiredData = data;
        }
    }

    private sealed class TestLevelUp : WiredAddonVariableLevelUp
    {
        public TestLevelUp(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx)
        {
            // The curve's params are covered by the tail rule, and GetIntParam looks the rule up
            // before the value -- fixed rules alone index past the end and throw on hydration.
            List<IWiredParamRule> rules = GetIntParamRules();
            IWiredParamRule? tail = GetIntParamTailRule();

            while (tail is not null && rules.Count < data.IntParams.Count)
            {
                rules.Add(tail);
            }

            data.AttatchRules(rules);
            _wiredData = data;
        }
    }

    /// <summary>A real "variable changed" trigger that keeps every change it is asked about, so a
    /// change that never reaches one is visible as an empty list rather than as nothing at all.
    /// </summary>
    private sealed class RecordingTrigger(List<WiredVariableChangedEvent> offered)
        : WiredTriggerVariableChanged(
            null!,
            new StuffDataFactory(),
            WiredTestBoxes.Context(99, tileIdx: 1)
        )
    {
        public override Task<bool> CanTriggerAsync(
            IWiredProcessingContext ctx,
            CancellationToken ct
        )
        {
            if (ctx.Event is WiredVariableChangedEvent evt)
            {
                offered.Add(evt);
            }

            return Task.FromResult(false);
        }

        protected override Task FillInternalDataAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
