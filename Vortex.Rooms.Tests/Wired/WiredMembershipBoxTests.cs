using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Furniture;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Selectors;
using Vortex.Rooms.Wired;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The three wired boxes that ask something about the person who triggered the stack — their guild,
/// their badge, what they are holding. All three used to be shells: the box saved its configuration,
/// the client drew it, and the evaluation always answered false, which reads in-game as "the wired
/// is broken" rather than as an unbuilt feature.
/// <para>
/// The guild and badge answers come from a room-side cache the box warms in <c>PrepareAsync</c>,
/// because a condition evaluates synchronously. These tests pin both halves: that the box asks for
/// the right thing, and that it answers from what it was given.
/// </para>
/// </summary>
public sealed class WiredMembershipBoxTests
{
    private static readonly RoomId Room = new(1);

    private static readonly PlayerId Triggerer = new(7);

    private static readonly PlayerId Bystander = new(8);

    [Fact]
    public void HandItem_MatchesOnlyTheConfiguredItem()
    {
        FakeLookup lookup = new(Player(Triggerer, carryItemId: 5));
        TestHandItemCondition condition = new(StubContext(lookup: lookup), Params(5));

        condition.Evaluate(Trigger()).Should().BeTrue();
    }

    [Fact]
    public void HandItem_DoesNotMatchADifferentItem()
    {
        FakeLookup lookup = new(Player(Triggerer, carryItemId: 5));
        TestHandItemCondition condition = new(StubContext(lookup: lookup), Params(2));

        condition.Evaluate(Trigger()).Should().BeFalse();
    }

    [Fact]
    public void HandItem_CodeZero_AsksForEmptyHanded()
    {
        // handitem0 is "None" on the client's own dropdown, so 0 is a real choice and not a
        // "leave this unset" sentinel.
        FakeLookup empty = new(Player(Triggerer, carryItemId: 0));
        FakeLookup holding = new(Player(Triggerer, carryItemId: 5));

        new TestHandItemCondition(StubContext(lookup: empty), Params(0))
            .Evaluate(Trigger())
            .Should()
            .BeTrue();

        new TestHandItemCondition(StubContext(lookup: holding), Params(0))
            .Evaluate(Trigger())
            .Should()
            .BeFalse();
    }

    [Fact]
    public void HandItem_NegativeVariant_FlipsTheAnswer()
    {
        FakeLookup lookup = new(Player(Triggerer, carryItemId: 5));

        new TestNegativeHandItemCondition(StubContext(lookup: lookup), Params(5))
            .Evaluate(Trigger())
            .Should()
            .BeFalse();

        new TestNegativeHandItemCondition(StubContext(lookup: lookup), Params(2))
            .Evaluate(Trigger())
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task HandItemSelector_PicksOnlyTheHolders()
    {
        FakeLookup lookup = new(
            Player(Triggerer, carryItemId: 5),
            Player(Bystander, carryItemId: 0)
        );
        TestHandItemSelector selector = new(StubContext(lookup: lookup), Params(5));

        IWiredSelectionSet set = await selector.SelectAsync(Trigger(), CancellationToken.None);

        set.SelectedPlayerIds.Should().BeEquivalentTo([(int)Triggerer]);
    }

    [Fact]
    public async Task Group_EmptyParam_AsksForTheRoomsOwnGuild()
    {
        FakeFurniAccess furni = new();
        TestGroupCondition condition = new(
            StubContext(groupId: 42, furni: furni, lookup: new FakeLookup()),
            Config(stringParam: string.Empty)
        );

        await condition.PrepareAsync(Trigger(), CancellationToken.None);

        furni.RostersRequested.Should().BeEquivalentTo([42]);
    }

    [Fact]
    public async Task Group_ExplicitParam_AsksForThatGuild()
    {
        FakeFurniAccess furni = new();
        TestGroupCondition condition = new(
            StubContext(groupId: 42, furni: furni, lookup: new FakeLookup()),
            Config(stringParam: "9")
        );

        await condition.PrepareAsync(Trigger(), CancellationToken.None);

        furni.RostersRequested.Should().BeEquivalentTo([9]);
    }

    [Fact]
    public void Group_PassesForAMember_AndFailsForEveryoneElse()
    {
        FakeFurniAccess furni = new();
        furni.Members[42] = [Triggerer];

        IRoomFloorItemContext ctx = StubContext(
            groupId: 42,
            furni: furni,
            lookup: new FakeLookup()
        );

        new TestGroupCondition(ctx, Config(string.Empty)).Evaluate(Trigger()).Should().BeTrue();
        new TestGroupCondition(ctx, Config(string.Empty))
            .Evaluate(Trigger(Bystander))
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Group_WithNoResolvableGuild_Fails()
    {
        FakeFurniAccess furni = new();
        furni.Members[42] = [Triggerer];

        // "Current group" in a room that belongs to no guild: nothing to be a member of.
        new TestGroupCondition(
            StubContext(groupId: null, furni: furni, lookup: new FakeLookup()),
            Config(string.Empty)
        )
            .Evaluate(Trigger())
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Group_NegativeVariant_FlipsTheAnswer()
    {
        FakeFurniAccess furni = new();
        furni.Members[42] = [Triggerer];

        IRoomFloorItemContext ctx = StubContext(
            groupId: 42,
            furni: furni,
            lookup: new FakeLookup()
        );

        new TestNegativeGroupCondition(ctx, Config(string.Empty))
            .Evaluate(Trigger())
            .Should()
            .BeFalse();

        new TestNegativeGroupCondition(ctx, Config(string.Empty))
            .Evaluate(Trigger(Bystander))
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task GroupSelector_LoadsOneRoster_AndPicksTheMembersInTheRoom()
    {
        FakeFurniAccess furni = new();
        furni.Members[42] = [Triggerer];

        FakeLookup lookup = new(Player(Triggerer), Player(Bystander));
        TestGroupSelector selector = new(
            StubContext(groupId: 42, furni: furni, lookup: lookup),
            Config(string.Empty)
        );

        IWiredSelectionSet set = await selector.SelectAsync(Trigger(), CancellationToken.None);

        set.SelectedPlayerIds.Should().BeEquivalentTo([(int)Triggerer]);

        // One roster load for the whole room, not one membership query per avatar.
        furni.RostersRequested.Should().BeEquivalentTo([42]);
    }

    [Fact]
    public async Task Badge_WarmsOnlyWhenACodeIsConfigured()
    {
        FakeFurniAccess configured = new();
        FakeFurniAccess blank = new();

        await new TestBadgeCondition(
            StubContext(furni: configured, lookup: new FakeLookup()),
            Config("ADM")
        ).PrepareAsync(Trigger(), CancellationToken.None);

        await new TestBadgeCondition(
            StubContext(furni: blank, lookup: new FakeLookup()),
            Config(string.Empty)
        ).PrepareAsync(Trigger(), CancellationToken.None);

        configured.BadgesRequested.Should().BeEquivalentTo([Triggerer]);
        blank.BadgesRequested.Should().BeEmpty();
    }

    [Fact]
    public void Badge_PassesOnlyForAWornCode()
    {
        FakeFurniAccess furni = new();
        furni.WornBadges[Triggerer] = ["ADM"];

        IRoomFloorItemContext ctx = StubContext(furni: furni, lookup: new FakeLookup());

        new TestBadgeCondition(ctx, Config("ADM")).Evaluate(Trigger()).Should().BeTrue();
        new TestBadgeCondition(ctx, Config("HC1")).Evaluate(Trigger()).Should().BeFalse();
        // An unconfigured box matches nobody rather than everybody.
        new TestBadgeCondition(ctx, Config(string.Empty))
            .Evaluate(Trigger())
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Badge_NegativeVariant_FlipsTheAnswer()
    {
        FakeFurniAccess furni = new();
        furni.WornBadges[Triggerer] = ["ADM"];

        IRoomFloorItemContext ctx = StubContext(furni: furni, lookup: new FakeLookup());

        new TestNegativeBadgeCondition(ctx, Config("ADM")).Evaluate(Trigger()).Should().BeFalse();
        new TestNegativeBadgeCondition(ctx, Config("HC1")).Evaluate(Trigger()).Should().BeTrue();
    }

    // ---- harness -------------------------------------------------------------------------------

    private static IWiredProcessingContext Trigger(PlayerId? actor = null)
    {
        TestEvent evt = new()
        {
            RoomId = Room,
            CausedBy = ActionContext.CreateForPlayer(actor ?? Triggerer, Room),
        };

        return FakeProxy.Create<IWiredProcessingContext>(call =>
            call.Method.Name == "get_Event" ? evt : null
        );
    }

    private static WiredData Config(string stringParam) => new() { StringParam = stringParam };

    private static WiredData Params(int value) => new() { IntParams = [value] };

    private static IRoomPlayer Player(PlayerId playerId, int carryItemId = 0) =>
        FakeProxy.Create<IRoomPlayer>(call =>
            call.Method.Name switch
            {
                "get_PlayerId" => playerId,
                "get_CarryItemId" => carryItemId,
                _ => null,
            }
        );

    private static IRoomFloorItemContext StubContext(
        int? groupId = null,
        IRoomFurniAccess? furni = null,
        IRoomLookup? lookup = null
    )
    {
        FurnitureDefinitionSnapshot definition = new()
        {
            Id = 1,
            SpriteId = 1,
            Name = "wf_cnd_test",
            ProductType = ProductType.Floor,
            FurniCategory = FurnitureCategory.Default,
            LogicName = "wf_cnd_test",
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
                "get_GroupId" => groupId,
                "get_Furni" => furni,
                "get_Lookup" => lookup,
                _ => null,
            }
        );
    }

    private sealed record TestEvent : RoomEvent;

    /// <summary>Only the members the boxes under test reach for; the rest of the room's furni surface
    /// is not what these tests are about.</summary>
    private sealed class FakeFurniAccess : IRoomFurniAccess
    {
        public Dictionary<int, HashSet<PlayerId>> Members { get; } = [];

        public Dictionary<PlayerId, HashSet<string>> WornBadges { get; } = [];

        public List<int> RostersRequested { get; } = [];

        public List<PlayerId> BadgesRequested { get; } = [];

        public Task EnsureGuildRosterAsync(int groupId, CancellationToken ct)
        {
            RostersRequested.Add(groupId);

            return Task.CompletedTask;
        }

        public bool IsGuildMember(int groupId, PlayerId player) =>
            Members.TryGetValue(groupId, out HashSet<PlayerId>? members)
            && members.Contains(player);

        public Task EnsureWornBadgesAsync(PlayerId player, CancellationToken ct)
        {
            BadgesRequested.Add(player);

            return Task.CompletedTask;
        }

        public bool IsWearingBadge(PlayerId player, string badgeCode) =>
            WornBadges.TryGetValue(player, out HashSet<string>? worn) && worn.Contains(badgeCode);

        public Task<bool> ValidateFloorItemPlacementAsync(
            ActionContext ctx,
            RoomObjectId itemId,
            int x,
            int y,
            Rotation rot
        ) => Task.FromResult(true);

        public IWiredVariable? GetVariableById(WiredVariableId id) => null;

        public void ScheduleFlashRevert(RoomObjectId objectId) { }

        public void ResetTimers() { }

        public WiredVariableHash AllVariablesHash => default;

        public bool TryGetVariableStore(WiredVariableKey key, out IWiredKeyValueStore? store)
        {
            store = null;

            return false;
        }

        public Task<bool> KickUserFromWiredAsync(PlayerId targetPlayerId, CancellationToken ct) =>
            Task.FromResult(false);
    }

    private sealed class FakeLookup(params IRoomPlayer[] players) : IRoomLookup
    {
        private readonly IRoomPlayer[] _players = players;

        public IReadOnlyCollection<IRoomAvatar> Avatars => _players;

        public IReadOnlyCollection<IRoomItem> Items => [];

        public int AvatarCount => _players.Length;

        public IRoomAvatar? FindAvatarByPlayer(PlayerId playerId) =>
            _players.FirstOrDefault(p => p.PlayerId == playerId);

        public bool TryFindAvatarByPlayer(
            PlayerId playerId,
            [NotNullWhen(true)] out IRoomAvatar? avatar
        )
        {
            avatar = FindAvatarByPlayer(playerId);

            return avatar is not null;
        }

        public IRoomItem? FindItem(RoomObjectId objectId) => null;

        public IRoomAvatar? FindAvatar(RoomObjectId objectId) => null;

        public bool TryFindItem(RoomObjectId objectId, [NotNullWhen(true)] out IRoomItem? item)
        {
            item = null;

            return false;
        }

        public bool TryFindAvatar(
            RoomObjectId objectId,
            [NotNullWhen(true)] out IRoomAvatar? avatar
        )
        {
            avatar = null;

            return false;
        }
    }

    // The boxes take their configuration from the persisted wired data the room fills in; these
    // subclasses hand it over directly so the behaviour can be exercised without a live room.
    private sealed class TestHandItemCondition : WiredConditionHabboHasHanditem
    {
        public TestHandItemCondition(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx)
        {
            data.AttatchRules(GetIntParamRules());
            _wiredData = data;
        }
    }

    private sealed class TestNegativeHandItemCondition : WiredNegativeConditionHabboHasHanditem
    {
        public TestNegativeHandItemCondition(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx)
        {
            data.AttatchRules(GetIntParamRules());
            _wiredData = data;
        }
    }

    private sealed class TestHandItemSelector : WiredSelectorEntitiesWithHanditem
    {
        public TestHandItemSelector(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx)
        {
            data.AttatchRules(GetIntParamRules());
            _wiredData = data;
        }
    }

    private sealed class TestGroupCondition : WiredConditionGroupMember
    {
        public TestGroupCondition(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx) => _wiredData = data;
    }

    private sealed class TestNegativeGroupCondition : WiredNegativeConditionGroupMember
    {
        public TestNegativeGroupCondition(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx) => _wiredData = data;
    }

    private sealed class TestGroupSelector : WiredSelectorEntitiesInGroup
    {
        public TestGroupSelector(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx) => _wiredData = data;
    }

    private sealed class TestBadgeCondition : WiredConditionHabboHasWearingBadge
    {
        public TestBadgeCondition(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx) => _wiredData = data;
    }

    private sealed class TestNegativeBadgeCondition : WiredNegativeConditionHabboHasWearingBadge
    {
        public TestNegativeBadgeCondition(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx) => _wiredData = data;
    }
}
