using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Action;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents;
using Vortex.Rooms.Grains.Systems;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Variables;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Variables;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The six Variable FX add-ons, end to end: an add-on stacked on a variable declares a display, and
/// a change to that variable pushes what the display now reads.
/// <para>
/// Both halves matter and neither is visible from the other. A config nothing sends is a bar the
/// client never draws; a status keyed differently from the config is a bar that draws once and then
/// freezes, with nothing in any log.
/// </para>
/// </summary>
public sealed class WiredVariableFxTests
{
    private const int BoxId = 7;

    private const int AddonId = 8;

    private const int Tile = 0;

    [Fact]
    public async Task AnFxAddOnDeclaresADisplayForTheVariableItStandsOn()
    {
        Harness harness = new();

        await harness.ProcessAsync();

        WiredVariableFxConfigSnapshot config = harness
            .LastConfigs()
            .Should()
            .ContainSingle()
            .Subject;

        config
            .ConfigId.Should()
            .Be(AddonId, "the add-on's own object id is the display's identity");
        config.CategoryId.Should().Be(4, "boss bar is category 4");
        config.ShowMode.Should().Be(2);
        config.ShowOnMouseHover.Should().BeTrue();
        config.ShowDuration.Should().Be(4000);
        config.ColorId.Should().Be(6);
        config.RendererId.Should().Be(9);
        config.DefaultMinValue.Should().Be(0);
        config.DefaultMaxValue.Should().Be(100);
    }

    [Fact]
    public async Task ChangingTheVariablePushesWhatTheDisplayReads()
    {
        Harness harness = new();

        await harness.ProcessAsync();
        await harness.ChangeVariableAsync(targetId: 42, value: 75);

        WiredVariableFxStatusSnapshot status = harness
            .LastStatuses()
            .Should()
            .ContainSingle()
            .Subject;

        status.EntityId.Should().Be(42);
        status.Value.Should().Be(75);
        status.IsUserEntity.Should().BeTrue();

        // The key has to name the same config the declaration carried, or the client draws nothing
        // and says nothing.
        status
            .StatusKey.Should()
            .Be(WiredVariableFxKey.Build(AddonId, harness.VariableId.ToString(), true, 42));
    }

    [Fact]
    public async Task AChangeToAnotherVariablePushesNothing()
    {
        Harness harness = new();

        await harness.ProcessAsync();

        int before = harness.Room.RoomComposers.Count;

        await harness.ChangeVariableAsync(
            targetId: 42,
            value: 1,
            variableId: WiredVariableIdBuilder.CreateFromBoxId(999)
        );

        harness
            .Room.RoomComposers.Count.Should()
            .Be(before, "no display is bound to that variable");
    }

    [Fact]
    public async Task TakingTheBoxAwayTakesItsDisplayToo()
    {
        Harness harness = new();

        await harness.ProcessAsync();
        harness.Room.RemoveItemOnly(new RoomObjectId(BoxId));

        await harness.ProcessAsync(2_000);

        harness
            .LastConfigs()
            .Should()
            .BeEmpty(
                "the client replaces its table from the set, so an empty one removes the last"
            );
    }

    private sealed class Harness
    {
        public FakeWiredRoomHost Room { get; } = new();

        public WiredVariableId VariableId { get; }

        private readonly RoomWiredSystem _engine;

        public Harness()
        {
            TestVariable variable = new(
                WiredTestBoxes.Context(BoxId),
                new WiredData { StringParam = "hp" }
            );

            VariableId = variable.GetVarSnapshot().VariableId;

            // The form the client sends, in its own order: source type, visibility, show mode,
            // _SafeStr_5289, hover, duration, style, colour, _SafeStr_5624, renderer, then the two
            // value-or-variable pairs and the override block.
            WiredData addonData = new()
            {
                IntParams = [1, 0, 2, 3, 1, 4000, 5, 6, 7, 9, 0, 0, 0, 100, 0, 0, 0, 0, 0, 0, 0],
            };

            TestBossBar addon = new(WiredTestBoxes.Context(AddonId), addonData);

            addonData.AttatchRules(addon.GetIntParamRules());

            Room.With(Floor(BoxId, variable), Tile).With(Floor(AddonId, addon), Tile);

            _engine = new RoomWiredSystem(Room);
        }

        public async Task ProcessAsync(long now = 1_000)
        {
            await _engine.OnRoomEventAsync(
                new WiredVariableBoxChangedEvent
                {
                    RoomId = new RoomId(1),
                    CausedBy = ActionContext.Wired,
                    BoxIds = [BoxId],
                },
                CancellationToken.None
            );

            await _engine.ProcessWiredAsync(now, CancellationToken.None);
        }

        public Task ChangeVariableAsync(
            int targetId,
            int value,
            WiredVariableId? variableId = null
        ) =>
            _engine.OnRoomEventAsync(
                new WiredVariableChangedEvent
                {
                    RoomId = new RoomId(1),
                    CausedBy = ActionContext.Wired,
                    Key = new WiredVariableKey(
                        variableId ?? VariableId,
                        WiredVariableTargetType.User,
                        targetId
                    ),
                    Kind = WiredVariableChangeKind.ValueChanged,
                    Previous = 0,
                    Current = value,
                },
                CancellationToken.None
            );

        public IReadOnlyList<WiredVariableFxConfigSnapshot> LastConfigs() =>
            Room.RoomComposers.OfType<VariableFxConfigUpdateMessageComposer>()
                .LastOrDefault()
                ?.Configs.ToList()
            ?? [];

        public IReadOnlyList<WiredVariableFxStatusSnapshot> LastStatuses() =>
            Room.RoomComposers.OfType<VariableFxStatusUpdateMessageComposer>()
                .LastOrDefault()
                ?.Statuses.ToList()
            ?? [];

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

    private sealed class TestVariable : WiredVariableReference
    {
        public TestVariable(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx) => _wiredData = data;
    }

    private sealed class TestBossBar : WiredAddonVariableFxBossBar
    {
        public TestBossBar(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx) => _wiredData = data;
    }
}
