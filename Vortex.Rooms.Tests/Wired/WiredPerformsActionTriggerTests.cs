using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Furniture;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Events.Player;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;
using Vortex.Rooms.Wired;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// "The user performs an action" (trigger 16), which had a form on the client and no behaviour here.
/// </summary>
/// <remarks>
/// The interesting part is the extra: the form encodes the chosen sign as bare digits and the chosen
/// dance as <c>"dance N"</c>, and an empty string means "any". Read the two the same way and a box
/// set to dance #3 fires for every dance.
/// </remarks>
public sealed class WiredPerformsActionTriggerTests
{
    private const int Wave = 0;

    private const int Sign = 10;

    private const int Dance = 11;

    [Fact]
    public async Task TheConfiguredAction_Fires_AndSelectsThePerformer()
    {
        TestTrigger trigger = new(new WiredData { IntParams = [Wave] });
        WiredSelectionSet selected = new();

        bool fired = await trigger.CanTriggerAsync(
            Processing(Performed(Wave, playerId: 7), selected),
            CancellationToken.None
        );

        fired.Should().BeTrue();
        selected.SelectedPlayerIds.Should().BeEquivalentTo([7]);
    }

    [Fact]
    public async Task AnotherAction_DoesNotFire()
    {
        TestTrigger trigger = new(new WiredData { IntParams = [Wave] });

        bool fired = await trigger.CanTriggerAsync(
            Processing(Performed(Dance, extra: 1)),
            CancellationToken.None
        );

        fired.Should().BeFalse();
    }

    [Fact]
    public async Task ADanceFilter_FiresOnlyForThatDance()
    {
        TestTrigger trigger = new(new WiredData { IntParams = [Dance], StringParam = "dance 3" });

        bool wrongDance = await trigger.CanTriggerAsync(
            Processing(Performed(Dance, extra: 1)),
            CancellationToken.None
        );
        bool rightDance = await trigger.CanTriggerAsync(
            Processing(Performed(Dance, extra: 3)),
            CancellationToken.None
        );

        wrongDance.Should().BeFalse();
        rightDance.Should().BeTrue();
    }

    [Fact]
    public async Task NoFilter_FiresForEveryDance()
    {
        TestTrigger trigger = new(new WiredData { IntParams = [Dance], StringParam = "" });

        bool fired = await trigger.CanTriggerAsync(
            Processing(Performed(Dance, extra: 4)),
            CancellationToken.None
        );

        fired.Should().BeTrue();
    }

    /// <summary>A sign carries the bare number, with none of the dance's prefix.</summary>
    [Fact]
    public async Task ASignFilter_ReadsTheBareNumber()
    {
        TestTrigger trigger = new(new WiredData { IntParams = [Sign], StringParam = "7" });

        bool wrongSign = await trigger.CanTriggerAsync(
            Processing(Performed(Sign, extra: 2)),
            CancellationToken.None
        );
        bool rightSign = await trigger.CanTriggerAsync(
            Processing(Performed(Sign, extra: 7)),
            CancellationToken.None
        );

        wrongSign.Should().BeFalse();
        rightSign.Should().BeTrue();
    }

    /// <summary>A box nobody configured has no action to watch for, and must not fire on all of them.</summary>
    [Fact]
    public async Task AnUnconfiguredBox_NeverFires()
    {
        TestTrigger trigger = new(new WiredData());

        bool fired = await trigger.CanTriggerAsync(
            Processing(Performed(Wave)),
            CancellationToken.None
        );

        fired.Should().BeFalse();
    }

    // ---- harness -------------------------------------------------------------------------------

    private static PlayerPerformedActionEvent Performed(
        int actionCode,
        int extra = -1,
        int playerId = 7
    ) =>
        new()
        {
            RoomId = 1,
            PlayerId = playerId,
            ActionCode = actionCode,
            Extra = extra,
        };

    private static IWiredProcessingContext Processing(
        PlayerPerformedActionEvent evt,
        WiredSelectionSet? selected = null
    )
    {
        WiredSelectionSet set = selected ?? new WiredSelectionSet();

        return FakeProxy.Create<IWiredProcessingContext>(call =>
            call.Method.Name switch
            {
                "get_Event" => evt,
                "get_Selected" => set,
                _ => null,
            }
        );
    }

    private sealed class TestTrigger : WiredTriggerHabboPerformsAction
    {
        public TestTrigger(WiredData data)
            : base(null!, new StuffDataFactory(), StubContext())
        {
            data.AttatchRules(GetIntParamRules());
            _wiredData = data;
        }

        private static IRoomFloorItemContext StubContext()
        {
            FurnitureDefinitionSnapshot definition = new()
            {
                Id = 1,
                SpriteId = 1,
                Name = "wf_trg_user_performs_action",
                ProductType = ProductType.Floor,
                FurniCategory = FurnitureCategory.Default,
                LogicName = "wf_trg_user_performs_action",
                TotalStates = 1,
                Width = 1,
                Length = 1,
                StackHeight = default,
                CanStack = false,
                CanWalk = false,
                CanSit = false,
                CanLay = false,
                CanRecycle = false,
                CanTrade = false,
                CanGroup = false,
                CanSell = false,
                UsagePolicy = FurnitureUsageType.Everybody,
                ExtraData = null,
                StuffDataType = StuffDataType.LegacyKey,
            };

            IRoomFloorItem item = FakeProxy.Create<IRoomFloorItem>(call =>
                call.Method.Name == "get_ExtraData" ? new ExtraData(null) : null
            );

            return FakeProxy.Create<IRoomFloorItemContext>(call =>
                call.Method.Name switch
                {
                    "get_Definition" => definition,
                    "get_RoomObject" => item,
                    _ => null,
                }
            );
        }
    }
}
