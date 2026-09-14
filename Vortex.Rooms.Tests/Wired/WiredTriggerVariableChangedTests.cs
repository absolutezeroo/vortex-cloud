using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Action;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains.Storage;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Variables;
using Vortex.Rooms.Wired;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The "variable changed" trigger, asked the one question the engine asks it: does this change
/// belong to the variable the player picked, and is it one of the ticked kinds.
/// </summary>
public sealed class WiredTriggerVariableChangedTests
{
    private const string VariableId = "4242";

    private static readonly RoomId Room = new(1);

    [Fact]
    public async Task AWriteToTheWatchedVariable_Fires()
    {
        TestTrigger trigger = Trigger([0, 1, 0, 0], VariableId);

        WiredSelectionSet selected = new();

        bool fired = await trigger.CanTriggerAsync(
            Ctx(Changed(WiredVariableChangeKind.ValueChanged, 0, 5), selected),
            CancellationToken.None
        );

        fired.Should().BeTrue();
        selected.SelectedPlayerIds.Should().Contain(7);
    }

    [Fact]
    public async Task AWriteToAnotherVariable_DoesNotFire()
    {
        TestTrigger trigger = Trigger([0, 1, 0, 0], VariableId);

        bool fired = await trigger.CanTriggerAsync(
            Ctx(
                Changed(WiredVariableChangeKind.ValueChanged, 0, 5, "9999"),
                new WiredSelectionSet()
            ),
            CancellationToken.None
        );

        fired.Should().BeFalse();
    }

    /// <summary>
    /// The half of the loop the trigger cannot see: the box has to announce the write. A room
    /// variable always exists — the client disables its Created and Deleted boxes precisely because
    /// it does — so the first write to one is a value change, not a creation, and it has to land.
    /// </summary>
    [Fact]
    public async Task AFirstWriteToARoomVariable_LandsAndIsAnnounced()
    {
        List<RoomEvent> published = [];
        FakeFurniAccess furni = new() { VariableStore = new KeyValueStore() };

        TestRoomVariable box = new(
            WiredTestBoxes.Context(objectId: 12, furniAccess: furni, published: published.Add),
            new WiredData { IntParams = [(int)WiredAvailabilityType.RoomActive] }
        );

        WiredVariableKey key = new(
            box.GetVarSnapshot().VariableId,
            WiredVariableTargetType.Global,
            0
        );

        // Exactly what the change-variable action does: set first, give as the fallback.
        if (!await box.SetValueAsync(null!, key, new WiredVariableValue(5)))
        {
            await box.GiveValueAsync(key, new WiredVariableValue(5), replace: true);
        }

        box.TryGetValue(key, out WiredVariableValue stored).Should().BeTrue();
        stored.Value.Should().Be(5);

        published
            .OfType<WiredVariableChangedEvent>()
            .Should()
            .ContainSingle()
            .Which.Kind.Should()
            .Be(WiredVariableChangeKind.ValueChanged);
    }

    private static WiredVariableChangedEvent Changed(
        WiredVariableChangeKind kind,
        int previous,
        int current,
        string variableId = VariableId
    ) =>
        new()
        {
            RoomId = Room,
            CausedBy = ActionContext.CreateForWired(Room),
            Key = new WiredVariableKey(
                WiredVariableId.Parse(variableId),
                WiredVariableTargetType.User,
                7
            ),
            Kind = kind,
            Previous = previous,
            Current = current,
        };

    private static IWiredProcessingContext Ctx(
        WiredVariableChangedEvent evt,
        WiredSelectionSet selected
    ) =>
        FakeProxy.Create<IWiredProcessingContext>(call =>
            call.Method.Name switch
            {
                "get_Event" => evt,
                "get_Selected" => selected,
                _ => null,
            }
        );

    private static TestTrigger Trigger(int[] intParams, string variableId) =>
        new(
            WiredTestBoxes.Context(),
            new WiredData { IntParams = [.. intParams], VariableIds = [variableId] }
        );

    private sealed class TestTrigger : WiredTriggerVariableChanged
    {
        public TestTrigger(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx)
        {
            data.AttatchRules(GetIntParamRules());
            _wiredData = data;
        }
    }

    private sealed class TestRoomVariable : WiredVariableRoom
    {
        public TestRoomVariable(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx)
        {
            data.AttatchRules(GetIntParamRules());
            _wiredData = data;
        }
    }
}
