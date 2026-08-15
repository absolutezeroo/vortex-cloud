using System;
using System.Collections.Generic;
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
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;
using Vortex.Rooms.Wired;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The boxes that read a wired variable back and the one that changes it. The room could already
/// write variables — give, remove, select by — but nothing could ask what one held and nothing
/// could do arithmetic on it, which left every counter, score and progression a player might build
/// write-only and fixed.
/// </summary>
public sealed class WiredVariableBoxTests
{
    private static readonly RoomId Room = new(1);

    private static readonly PlayerId Triggerer = new(7);

    private const string VariableId = "4242";

    private const int Furni = 91;

    [Fact]
    public async Task HasVariable_PassesWhenTheUserHoldsIt_AndFailsWhenNobodyDoes()
    {
        FakeVariable variable = new(WiredVariableTargetType.User);
        variable.Values[Key(WiredVariableTargetType.User, (int)Triggerer)] = new(3);

        TestHasVariable held = Has(variable, WiredVariableTargetType.User);
        await held.PrepareAsync(Trigger(players: [Triggerer]), CancellationToken.None);
        held.Evaluate(Trigger()).Should().BeTrue();

        TestHasVariable other = Has(variable, WiredVariableTargetType.User);
        await other.PrepareAsync(Trigger(players: [new(8)]), CancellationToken.None);
        other.Evaluate(Trigger()).Should().BeFalse();
    }

    [Fact]
    public async Task HasVariable_NegativeVariant_FlipsTheAnswer()
    {
        FakeVariable variable = new(WiredVariableTargetType.User);

        TestNegativeHasVariable box = new(
            StubContext(variable),
            Config(WiredVariableTargetType.User)
        );

        await box.PrepareAsync(Trigger(players: [Triggerer]), CancellationToken.None);

        // Nothing was ever written for that user, so the positive reading is false.
        box.Evaluate(Trigger()).Should().BeTrue();
    }

    [Fact]
    public async Task HasVariable_RoomWideVariable_NeedsNoSelection()
    {
        FakeVariable variable = new(WiredVariableTargetType.Global);
        variable.Values[Key(WiredVariableTargetType.Global, 0)] = new(1);

        TestHasVariable box = Has(variable, WiredVariableTargetType.Global);

        // An empty stack selection must not stop a global variable from answering.
        await box.PrepareAsync(Trigger(), CancellationToken.None);

        box.Evaluate(Trigger()).Should().BeTrue();
    }

    [Fact]
    public async Task HasVariable_WithoutPreparing_DoesNotPass()
    {
        FakeVariable variable = new(WiredVariableTargetType.User);
        variable.Values[Key(WiredVariableTargetType.User, (int)Triggerer)] = new(3);

        TestHasVariable box = Has(variable, WiredVariableTargetType.User);

        box.Evaluate(Trigger()).Should().BeFalse();
    }

    [Theory]
    // value 10 against a literal 10, over the six operators the client offers.
    [InlineData(WiredComparisonType.Equals, 10, true)]
    [InlineData(WiredComparisonType.NotEquals, 10, false)]
    [InlineData(WiredComparisonType.GreaterThan, 10, false)]
    [InlineData(WiredComparisonType.GreaterTHanOrEquals, 10, true)]
    [InlineData(WiredComparisonType.LessThan, 10, false)]
    [InlineData(WiredComparisonType.LessThanOrEquals, 10, true)]
    [InlineData(WiredComparisonType.GreaterThan, 4, true)]
    [InlineData(WiredComparisonType.LessThan, 40, true)]
    public async Task VariableValue_ComparesAgainstTheLiteral(
        WiredComparisonType comparison,
        int literal,
        bool expected
    )
    {
        FakeVariable variable = new(WiredVariableTargetType.User);
        variable.Values[Key(WiredVariableTargetType.User, (int)Triggerer)] = new(10);

        TestVariableValue box = new(
            StubContext(variable),
            ValueConfig(comparison, literal: literal)
        );

        await box.PrepareAsync(Trigger(players: [Triggerer]), CancellationToken.None);

        box.Evaluate(Trigger()).Should().Be(expected);
    }

    [Fact]
    public async Task VariableValue_ReadsANegativeLiteralFromBothHalves()
    {
        FakeVariable variable = new(WiredVariableTargetType.User);
        variable.Values[Key(WiredVariableTargetType.User, (int)Triggerer)] = new(-5);

        TestVariableValue box = new(
            StubContext(variable),
            ValueConfig(WiredComparisonType.Equals, literal: -5)
        );

        await box.PrepareAsync(Trigger(players: [Triggerer]), CancellationToken.None);

        box.Evaluate(Trigger()).Should().BeTrue();
    }

    [Fact]
    public async Task VariableValue_AgainstAnotherVariable_ComparesTheTwoValues()
    {
        FakeVariable variable = new(WiredVariableTargetType.User);
        variable.Values[Key(WiredVariableTargetType.User, (int)Triggerer)] = new(10);

        // Reference mode: the literal halves are ignored and variable id [1] is read instead.
        TestVariableValue box = new(
            StubContext(variable),
            ValueConfig(WiredComparisonType.Equals, literal: 999, referenceVariableId: VariableId)
        );

        await box.PrepareAsync(Trigger(players: [Triggerer]), CancellationToken.None);

        box.Evaluate(Trigger()).Should().BeTrue();
    }

    [Fact]
    public async Task VariableValue_WithAnUnsetVariable_Fails()
    {
        // The store pre-fills its out parameter with WiredVariableValue.Default (1) on a miss, so a
        // box comparing "= 1" would pass on a variable that was never written if the miss leaked.
        FakeVariable variable = new(WiredVariableTargetType.User);

        TestVariableValue box = new(
            StubContext(variable),
            ValueConfig(WiredComparisonType.Equals, literal: 1)
        );

        await box.PrepareAsync(Trigger(players: [Triggerer]), CancellationToken.None);

        box.Evaluate(Trigger()).Should().BeFalse();
    }

    [Fact]
    public async Task VariableValue_ReferenceVariableThatHoldsNothing_Fails()
    {
        FakeVariable variable = new(WiredVariableTargetType.User);
        variable.Values[Key(WiredVariableTargetType.User, (int)Triggerer)] = new(0);

        // Comparing "= 0" against an absent reference must not silently compare against zero.
        TestVariableValue box = new(
            StubContext(variable),
            ValueConfig(WiredComparisonType.Equals, literal: 0, referenceVariableId: "9999")
        );

        await box.PrepareAsync(Trigger(players: [Triggerer]), CancellationToken.None);

        box.Evaluate(Trigger()).Should().BeFalse();
    }

    [Fact]
    public async Task VariableValue_OnFurni_ReadsThePerFurniValue()
    {
        FakeVariable variable = new(WiredVariableTargetType.Furni);
        variable.Values[Key(WiredVariableTargetType.Furni, Furni)] = new(7);

        TestVariableValue box = new(
            StubContext(variable),
            ValueConfig(
                WiredComparisonType.Equals,
                literal: 7,
                target: WiredVariableTargetType.Furni
            )
        );

        await box.PrepareAsync(Trigger(furni: [Furni]), CancellationToken.None);

        box.Evaluate(Trigger()).Should().BeTrue();
    }

    [Fact]
    public async Task ChangeVariable_FirstWriteCreatesTheValue_ThenAccumulates()
    {
        FakeVariable variable = new(WiredVariableTargetType.User);

        TestChangeVariable box = new(
            StubContext(variable),
            ChangeConfig(WiredVariableOperation.Add, operand: 5)
        );

        // SetValueAsync only updates a key that already exists, so without the Give fallback the
        // very first "add 5" would be dropped and the variable would stay absent.
        await box.ExecuteAsync(Execution(players: [Triggerer]), CancellationToken.None);
        await box.ExecuteAsync(Execution(players: [Triggerer]), CancellationToken.None);

        variable.Values[Key(WiredVariableTargetType.User, (int)Triggerer)].Value.Should().Be(10);
    }

    [Fact]
    public async Task ChangeVariable_StartsFromZero_NotFromTheStoresDefault()
    {
        FakeVariable variable = new(WiredVariableTargetType.User);

        TestChangeVariable box = new(
            StubContext(variable),
            ChangeConfig(WiredVariableOperation.Add, operand: 5)
        );

        await box.ExecuteAsync(Execution(players: [Triggerer]), CancellationToken.None);

        // WiredVariableValue.Default is 1: reading the miss would make this 6.
        variable.Values[Key(WiredVariableTargetType.User, (int)Triggerer)].Value.Should().Be(5);
    }

    [Fact]
    public async Task ChangeVariable_WritesEveryTargetInTheSelection()
    {
        FakeVariable variable = new(WiredVariableTargetType.Furni);

        TestChangeVariable box = new(
            StubContext(variable),
            ChangeConfig(
                WiredVariableOperation.Assign,
                operand: 3,
                target: WiredVariableTargetType.Furni
            )
        );

        await box.ExecuteAsync(Execution(furni: [Furni, Furni + 1]), CancellationToken.None);

        variable.Values[Key(WiredVariableTargetType.Furni, Furni)].Value.Should().Be(3);
        variable.Values[Key(WiredVariableTargetType.Furni, Furni + 1)].Value.Should().Be(3);
    }

    [Fact]
    public async Task ChangeVariable_ReferenceOperandThatHoldsNothing_WritesNothing()
    {
        FakeVariable variable = new(WiredVariableTargetType.User);

        TestChangeVariable box = new(
            StubContext(variable),
            ChangeConfig(WiredVariableOperation.Assign, operand: 7, referenceVariableId: "9999")
        );

        await box.ExecuteAsync(Execution(players: [Triggerer]), CancellationToken.None);

        // Assigning the literal instead of the missing reference would write a number the box was
        // never configured with.
        variable.Values.Should().BeEmpty();
    }

    [Fact]
    public async Task ChangeVariable_UnknownOperation_LeavesTheValueAlone()
    {
        FakeVariable variable = new(WiredVariableTargetType.User);
        variable.Values[Key(WiredVariableTargetType.User, (int)Triggerer)] = new(4);

        TestChangeVariable box = new(
            StubContext(variable),
            ChangeConfig((WiredVariableOperation)111, operand: 5)
        );

        await box.ExecuteAsync(Execution(players: [Triggerer]), CancellationToken.None);

        variable.Values[Key(WiredVariableTargetType.User, (int)Triggerer)].Value.Should().Be(4);
    }

    [Fact]
    public async Task VariableAge_OlderThan_ComparesAgainstTheChosenMoment()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        FakeVariable variable = new(WiredVariableTargetType.User);
        WiredVariableKey key = Key(WiredVariableTargetType.User, (int)Triggerer);

        variable.Values[key] = new(1);
        // Created two hours ago, written again a minute ago.
        variable.Stamps[key] = (now - (2 * 3600 * 1000), now - 60_000);

        TestVariableAge fromCreation = new(
            StubContext(variable),
            AgeConfig(WiredComparisonType.GreaterThan, 1, WiredTimeUnit.Hours, fromCreation: true)
        );

        await fromCreation.PrepareAsync(Trigger(players: [Triggerer]), CancellationToken.None);
        fromCreation.Evaluate(Trigger()).Should().BeTrue();

        // The same box measured from the last write is only a minute old.
        TestVariableAge fromUpdate = new(
            StubContext(variable),
            AgeConfig(WiredComparisonType.GreaterThan, 1, WiredTimeUnit.Hours, fromCreation: false)
        );

        await fromUpdate.PrepareAsync(Trigger(players: [Triggerer]), CancellationToken.None);
        fromUpdate.Evaluate(Trigger()).Should().BeFalse();
    }

    [Fact]
    public async Task VariableAge_YoungerThan_IsTheOtherComparison()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        FakeVariable variable = new(WiredVariableTargetType.User);
        WiredVariableKey key = Key(WiredVariableTargetType.User, (int)Triggerer);

        variable.Values[key] = new(1);
        variable.Stamps[key] = (now - 60_000, now - 60_000);

        TestVariableAge box = new(
            StubContext(variable),
            AgeConfig(WiredComparisonType.LessThan, 1, WiredTimeUnit.Hours, fromCreation: true)
        );

        await box.PrepareAsync(Trigger(players: [Triggerer]), CancellationToken.None);

        box.Evaluate(Trigger()).Should().BeTrue();
    }

    [Fact]
    public async Task VariableAge_WithNoRecordedMoment_Fails()
    {
        // A value written before the room kept times has no stamp. Measuring its age from the epoch
        // would make it 56 years old and fire every "older than" box in the hotel.
        FakeVariable variable = new(WiredVariableTargetType.User);
        variable.Values[Key(WiredVariableTargetType.User, (int)Triggerer)] = new(1);

        TestVariableAge box = new(
            StubContext(variable),
            AgeConfig(WiredComparisonType.GreaterThan, 1, WiredTimeUnit.Seconds, fromCreation: true)
        );

        await box.PrepareAsync(Trigger(players: [Triggerer]), CancellationToken.None);

        box.Evaluate(Trigger()).Should().BeFalse();
    }

    // ---- harness -------------------------------------------------------------------------------

    private static WiredData AgeConfig(
        WiredComparisonType comparison,
        int duration,
        WiredTimeUnit unit,
        bool fromCreation
    ) =>
        new()
        {
            // [target, comparison, creation-or-update, long high, long low, unit]
            IntParams =
            [
                (int)WiredVariableTargetType.User,
                (int)comparison,
                fromCreation ? 0 : 1,
                duration < 0 ? -1 : 0,
                duration,
                (int)unit,
            ],
            VariableIds = [VariableId],
        };

    private static WiredData ChangeConfig(
        WiredVariableOperation operation,
        int operand,
        string? referenceVariableId = null,
        WiredVariableTargetType target = WiredVariableTargetType.User
    ) =>
        new()
        {
            IntParams =
            [
                (int)target,
                (int)operation,
                referenceVariableId is null ? 0 : 1,
                operand < 0 ? -1 : 0,
                operand,
                (int)target,
            ],
            VariableIds = [VariableId, referenceVariableId ?? string.Empty],
        };

    private static IWiredExecutionContext Execution(PlayerId[]? players = null, int[]? furni = null)
    {
        WiredSelectionSet selection = Selection(players, furni);

        return FakeProxy.Create<IWiredExecutionContext>(call =>
            call.Method.Name == "GetEffectiveSelectionAsync"
                ? Task.FromResult<IWiredSelectionSet>(selection)
                : null
        );
    }

    private static WiredSelectionSet Selection(PlayerId[]? players, int[]? furni)
    {
        WiredSelectionSet selection = new();

        foreach (PlayerId player in players ?? [])
        {
            selection.SelectedPlayerIds.Add((int)player);
        }

        foreach (int id in furni ?? [])
        {
            selection.SelectedFurniIds.Add(id);
        }

        return selection;
    }

    private static WiredVariableKey Key(WiredVariableTargetType target, int targetId) =>
        new(WiredVariableId.Parse(VariableId), target, targetId);

    private static TestHasVariable Has(FakeVariable variable, WiredVariableTargetType target) =>
        new(StubContext(variable), Config(target));

    private static WiredData Config(WiredVariableTargetType target) =>
        new() { IntParams = [(int)target], VariableIds = [VariableId] };

    private static WiredData ValueConfig(
        WiredComparisonType comparison,
        int literal,
        string? referenceVariableId = null,
        WiredVariableTargetType target = WiredVariableTargetType.User
    ) =>
        new()
        {
            // [target, comparison, value-or-variable, long high, long low, reference target]
            IntParams =
            [
                (int)target,
                (int)comparison,
                referenceVariableId is null ? 0 : 1,
                literal < 0 ? -1 : 0,
                literal,
                (int)target,
            ],
            VariableIds = [VariableId, referenceVariableId ?? string.Empty],
        };

    private static IWiredProcessingContext Trigger(PlayerId[]? players = null, int[]? furni = null)
    {
        TestEvent evt = new()
        {
            RoomId = Room,
            CausedBy = ActionContext.CreateForPlayer(Triggerer, Room),
        };

        WiredSelectionSet selection = Selection(players, furni);

        return FakeProxy.Create<IWiredProcessingContext>(call =>
            call.Method.Name switch
            {
                "get_Event" => evt,
                "GetEffectiveSelectionAsync" => Task.FromResult<IWiredSelectionSet>(selection),
                _ => null,
            }
        );
    }

    private static IRoomFloorItemContext StubContext(IWiredVariable variable)
    {
        FurnitureDefinitionSnapshot definition = new()
        {
            Id = 1,
            SpriteId = 1,
            Name = "wf_cnd_var",
            ProductType = ProductType.Floor,
            FurniCategory = FurnitureCategory.Default,
            LogicName = "wf_cnd_var",
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

        IRoomFurniAccess furni = FakeProxy.Create<IRoomFurniAccess>(call =>
            call.Method.Name == "GetVariableById" ? variable : null
        );

        return FakeProxy.Create<IRoomFloorItemContext>(call =>
            call.Method.Name switch
            {
                "get_Definition" => definition,
                "get_RoomObject" => item,
                "get_Furni" => furni,
                _ => null,
            }
        );
    }

    private sealed record TestEvent : RoomEvent;

    /// <summary>A variable holding whatever the test put in it, keyed exactly like the real
    /// stores.</summary>
    private sealed class FakeVariable(WiredVariableTargetType target) : IWiredVariable
    {
        public Dictionary<WiredVariableKey, WiredVariableValue> Values { get; } = [];

        public bool CanBind(in WiredVariableKey key) => key.TargetType == target;

        public bool TryGetValue(in WiredVariableKey key, out WiredVariableValue value)
        {
            // Mirrors the real stores, which pre-fill the out parameter before reporting a miss.
            value = WiredVariableValue.Default;

            return Values.TryGetValue(key, out value);
        }

        public Task<bool> GiveValueAsync(
            WiredVariableKey key,
            WiredVariableValue value,
            bool replace = false
        )
        {
            Values[key] = value;

            return Task.FromResult(true);
        }

        public Task<bool> SetValueAsync(
            IWiredExecutionContext ctx,
            WiredVariableKey key,
            WiredVariableValue value
        )
        {
            Values[key] = value;

            return Task.FromResult(true);
        }

        public Dictionary<WiredVariableKey, (long Created, long Updated)> Stamps { get; } = [];

        public bool TryGetTimestamps(
            in WiredVariableKey key,
            out long createdAtMs,
            out long updatedAtMs
        )
        {
            createdAtMs = 0;
            updatedAtMs = 0;

            if (!Stamps.TryGetValue(key, out (long Created, long Updated) stamp))
            {
                return false;
            }

            (createdAtMs, updatedAtMs) = stamp;

            return true;
        }

        public bool RemoveValue(WiredVariableKey key) => Values.Remove(key);

        public WiredVariableSnapshot GetVarSnapshot() =>
            new()
            {
                VariableId = WiredVariableId.Parse(VariableId),
                VariableName = "test",
                VariableType = WiredVariableType.Internal,
                VariableHash = default,
                AvailabilityType = WiredAvailabilityType.Internal,
                TargetType = target,
                Flags = WiredVariableFlags.HasValue,
                TextConnectors = new(),
            };
    }

    private sealed class TestHasVariable : WiredConditionHasVariable
    {
        public TestHasVariable(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx)
        {
            data.AttatchRules(GetIntParamRules());
            _wiredData = data;
        }
    }

    private sealed class TestNegativeHasVariable : WiredNegativeConditionHasVariable
    {
        public TestNegativeHasVariable(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx)
        {
            data.AttatchRules(GetIntParamRules());
            _wiredData = data;
        }
    }

    private sealed class TestChangeVariable : WiredActionChangeVariable
    {
        public TestChangeVariable(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx)
        {
            data.AttatchRules(GetIntParamRules());
            _wiredData = data;
        }
    }

    private sealed class TestVariableAge : WiredConditionVariableAge
    {
        public TestVariableAge(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx)
        {
            data.AttatchRules(GetIntParamRules());
            _wiredData = data;
        }
    }

    private sealed class TestVariableValue : WiredConditionVariableValue
    {
        public TestVariableValue(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx)
        {
            data.AttatchRules(GetIntParamRules());
            _wiredData = data;
        }
    }
}
