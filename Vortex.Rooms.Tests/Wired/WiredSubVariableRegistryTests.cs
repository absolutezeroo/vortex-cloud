using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Orleans;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains.Systems;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Variables;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Variables;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// One variable box, several variables.
/// <para>
/// The registry used to keep exactly one variable id per box, which is what the boxes it knew about
/// needed. The whole "Variable ..." add-on family does not fit that: a level-up add-on turns one
/// experience value into level, progress, required and max, a time add-on turns one timestamp into a
/// calendar, and the six Variable FX boxes hang a display off any of them. They are add-ons because
/// they describe someone else's variable — they have no value of their own.
/// </para>
/// <para>
/// The property that matters on the way out is the one a 1:1 registry gets wrong for free: picking
/// the box up has to take every variable it put in the room, not just the one it declared.
/// </para>
/// </summary>
public sealed class WiredSubVariableRegistryTests
{
    private const int BoxId = 7;

    private const int AddonId = 8;

    private const int Tile = 0;

    [Fact]
    public async Task AnAddOnsDerivedVariablesAreRegisteredWithTheBox()
    {
        FakeWiredRoomHost room = new();
        Harness harness = new(room, "score", "score.level", "score.progress");
        RoomWiredSystem engine = new(room);

        await harness.ProcessAsync(engine);

        engine.GetVariableById(harness.ParentId).Should().NotBeNull();
        engine.GetVariableById(harness.DerivedIds[0]).Should().NotBeNull();
        engine.GetVariableById(harness.DerivedIds[1]).Should().NotBeNull();
    }

    [Fact]
    public async Task TakingTheBoxAwayTakesItsDerivedVariablesToo()
    {
        FakeWiredRoomHost room = new();
        Harness harness = new(room, "score", "score.level", "score.progress");
        RoomWiredSystem engine = new(room);

        await harness.ProcessAsync(engine);

        // The box leaves the room, and the next pass over it finds nothing.
        room.RemoveItemOnly(harness.BoxObjectId);

        await harness.ProcessAsync(engine, 2_000);

        engine.GetVariableById(harness.ParentId).Should().BeNull();
        engine
            .GetVariableById(harness.DerivedIds[0])
            .Should()
            .BeNull(
                "a derived variable outliving its box is a live id pointing at a dropped logic"
            );
        engine.GetVariableById(harness.DerivedIds[1]).Should().BeNull();
    }

    [Fact]
    public async Task AnAddOnThatDerivesNothingLeavesTheBoxAlone()
    {
        FakeWiredRoomHost room = new();
        Harness harness = new(room);
        RoomWiredSystem engine = new(room);

        await harness.ProcessAsync(engine);

        engine.GetVariableById(harness.ParentId).Should().NotBeNull();
    }

    [Fact]
    public async Task AnAddOnThatThrowsDoesNotCostTheBoxItsOwnVariable()
    {
        FakeWiredRoomHost room = new();
        Harness harness = new(room) { AddonThrows = true };
        RoomWiredSystem engine = new(room);

        await harness.ProcessAsync(engine);

        engine
            .GetVariableById(harness.ParentId)
            .Should()
            .NotBeNull("a broken add-on is not a reason for the variable itself to disappear");
    }

    /// <summary>A room holding one variable box with one contributing add-on stacked on it.</summary>
    private sealed class Harness
    {
        public RoomObjectId BoxObjectId { get; } = new(BoxId);

        public WiredVariableId ParentId { get; }

        public List<WiredVariableId> DerivedIds { get; } = [];

        public bool AddonThrows
        {
            get => _addon.Throws;
            set => _addon.Throws = value;
        }

        private readonly TestSubVariableAddon _addon;

        public Harness(FakeWiredRoomHost room, params string[] derivedNames)
        {
            TestRoomVariable parent = new(
                WiredTestBoxes.Context(BoxId),
                new WiredData { StringParam = "score" }
            );

            ParentId = parent.GetVarSnapshot().VariableId;

            _addon = new TestSubVariableAddon(WiredTestBoxes.Context(AddonId), new WiredData());

            for (int i = 0; i < derivedNames.Length; i++)
            {
                // Ids of their own, in the same space the room resolves every variable through.
                WiredVariableId id = WiredVariableIdBuilder.CreateFromBoxId(100 + i);

                DerivedIds.Add(id);
                _addon.Derived.Add(new StubVariable(id, derivedNames[i]));
            }

            room.With(Floor(BoxId, parent), Tile).With(Floor(AddonId, _addon), Tile);
        }

        public async Task ProcessAsync(RoomWiredSystem engine, long now = 1_000)
        {
            await engine.OnRoomEventAsync(
                new WiredVariableBoxChangedEvent
                {
                    RoomId = new RoomId(1),
                    CausedBy = ActionContext.Wired,
                    BoxIds = [BoxId],
                },
                CancellationToken.None
            );

            await engine.ProcessWiredAsync(now, CancellationToken.None);
        }

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

    private sealed class TestRoomVariable : WiredVariableReference
    {
        public TestRoomVariable(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx) => _wiredData = data;
    }

    /// <summary>An add-on that hands the room whatever variables the test gave it.</summary>
    private sealed class TestSubVariableAddon : FurnitureWiredAddonLogic, IWiredSubVariableSource
    {
        public List<IWiredVariable> Derived { get; } = [];

        public bool Throws { get; set; }

        public TestSubVariableAddon(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx) => _wiredData = data;

        public override int WiredCode => 9999;

        public IReadOnlyList<IWiredVariable> CreateSubVariables(IWiredVariable parent) =>
            Throws ? throw new System.InvalidOperationException("broken add-on") : Derived;
    }

    /// <summary>A derived variable: an id and a name are all the registry reads.</summary>
    private sealed class StubVariable(WiredVariableId id, string name) : IWiredVariable
    {
        public bool CanBind(in WiredVariableKey key) => key.VariableId == id;

        public WiredVariableSnapshot GetVarSnapshot() =>
            new()
            {
                VariableId = id,
                VariableName = name,
                VariableType = WiredVariableType.Created,
                VariableHash = default,
                AvailabilityType = WiredAvailabilityType.Reference,
                TargetType = WiredVariableTargetType.Global,
                Flags = WiredVariableFlags.HasValue,
                TextConnectors = [],
            };

        public bool TryGetValue(in WiredVariableKey key, out WiredVariableValue value)
        {
            value = WiredVariableValue.Default;

            return false;
        }

        public Task<bool> GiveValueAsync(
            WiredVariableKey key,
            WiredVariableValue value,
            bool replace = false
        ) => Task.FromResult(false);

        public Task<bool> SetValueAsync(
            IWiredExecutionContext ctx,
            WiredVariableKey key,
            WiredVariableValue value
        ) => Task.FromResult(false);

        public bool RemoveValue(WiredVariableKey key) => false;

        public bool TryGetTimestamps(
            in WiredVariableKey key,
            out long createdAtMs,
            out long updatedAtMs
        )
        {
            createdAtMs = 0;
            updatedAtMs = 0;

            return false;
        }
    }
}
