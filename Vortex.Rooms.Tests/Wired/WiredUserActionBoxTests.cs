using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Furniture;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Selectors;
using Vortex.Rooms.Wired;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// Two boxes that had a form and no behaviour: "the users performing an action" (a selector that
/// selected nobody) and "mute user" (an effect with nothing behind it). The first now answers
/// through the same matcher as its condition twin, so a posture cannot read one way in a condition
/// and the other way in a selector.
/// </summary>
public sealed class WiredUserActionBoxTests
{
    private const int Dancing = 11;

    private const int Standing = 7;

    [Fact]
    public async Task TheActionSelector_PicksOnlyTheAvatarsDoingIt()
    {
        IRoomPlayer dancer = Player(7, dancing: true);
        IRoomPlayer sitter = Player(8, sitting: true);

        TestActionSelector selector = new(
            StubContext(lookup: new FakeRoomLookup(dancer, sitter)),
            new WiredData { IntParams = [Dancing] }
        );

        IWiredSelectionSet set = await selector.SelectAsync(Processing(), CancellationToken.None);

        set.SelectedPlayerIds.Should().BeEquivalentTo([7]);
    }

    [Fact]
    public async Task Standing_IsTheAbsenceOfTheTwoPostures()
    {
        IRoomPlayer sitter = Player(8, sitting: true);
        IRoomPlayer stander = Player(9);

        TestActionSelector selector = new(
            StubContext(lookup: new FakeRoomLookup(sitter, stander)),
            new WiredData { IntParams = [Standing] }
        );

        IWiredSelectionSet set = await selector.SelectAsync(Processing(), CancellationToken.None);

        set.SelectedPlayerIds.Should().BeEquivalentTo([9]);
    }

    [Fact]
    public async Task AnUnconfiguredActionSelector_PicksNobody()
    {
        TestActionSelector selector = new(
            StubContext(lookup: new FakeRoomLookup(Player(7, dancing: true))),
            new WiredData()
        );

        IWiredSelectionSet set = await selector.SelectAsync(Processing(), CancellationToken.None);

        set.SelectedPlayerIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Mute_SilencesEverySelectedUser_ForTheConfiguredMinutes()
    {
        FakeFurniAccess furni = new();
        TestMuteUser box = new(
            StubContext(furni: furni),
            new WiredData { IntParams = [2], StringParam = string.Empty }
        );

        WiredSelectionSet selection = new();
        selection.SelectedPlayerIds.Add(7);
        selection.SelectedPlayerIds.Add(8);

        await box.ExecuteAsync(Execution(selection), CancellationToken.None);

        furni.Muted.Should().HaveCount(2);
        furni.Muted.Should().OnlyContain(m => m.DurationSeconds == 120);
    }

    [Fact]
    public async Task MuteOfZeroMinutes_MutesNobody()
    {
        // The slider allows 0, and a mute of no duration is not an unmute — reading it as one would
        // hand a room a way to lift a moderator's mute.
        FakeFurniAccess furni = new();
        TestMuteUser box = new(StubContext(furni: furni), new WiredData { IntParams = [0] });

        WiredSelectionSet selection = new();
        selection.SelectedPlayerIds.Add(7);

        await box.ExecuteAsync(Execution(selection), CancellationToken.None);

        furni.Muted.Should().BeEmpty();
    }

    // ---- harness -------------------------------------------------------------------------------

    private static IRoomPlayer Player(int playerId, bool dancing = false, bool sitting = false) =>
        FakeProxy.Create<IRoomPlayer>(call =>
            call.Method.Name switch
            {
                "get_PlayerId" => new PlayerId(playerId),
                "get_DanceType" => dancing ? AvatarDanceType.Dance : AvatarDanceType.None,
                "HasStatus" => sitting && HasSit(call.Args),
                _ => null,
            }
        );

    private static bool HasSit(object?[]? args) =>
        args is [AvatarStatusType[] types] && Array.IndexOf(types, AvatarStatusType.Sit) >= 0;

    private static IWiredProcessingContext Processing() =>
        FakeProxy.Create<IWiredProcessingContext>(_ => null);

    private static IWiredExecutionContext Execution(WiredSelectionSet selection) =>
        FakeProxy.Create<IWiredExecutionContext>(call =>
            call.Method.Name switch
            {
                "GetEffectiveSelectionAsync" => Task.FromResult<IWiredSelectionSet>(selection),
                "get_Addons" => new System.Collections.Generic.List<IWiredAddon>(),
                _ => null,
            }
        );

    private static IRoomFloorItemContext StubContext(
        IRoomLookup? lookup = null,
        IRoomFurniAccess? furni = null
    )
    {
        FurnitureDefinitionSnapshot definition = new()
        {
            Id = 1,
            SpriteId = 1,
            Name = "wf_box",
            ProductType = ProductType.Floor,
            FurniCategory = FurnitureCategory.Default,
            LogicName = "wf_box",
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
                "get_Lookup" => lookup,
                "get_Furni" => furni,
                _ => null,
            }
        );
    }

    private sealed class TestActionSelector : WiredSelectorEntitiesByAction
    {
        public TestActionSelector(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx)
        {
            data.AttatchRules(GetIntParamRules());
            _wiredData = data;
        }
    }

    private sealed class TestMuteUser : WiredActionMuteUser
    {
        public TestMuteUser(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx)
        {
            data.AttatchRules(GetIntParamRules());
            _wiredData = data;
        }
    }
}
