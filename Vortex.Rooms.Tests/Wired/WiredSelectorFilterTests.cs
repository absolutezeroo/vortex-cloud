using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Furniture;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;
using Vortex.Rooms.Wired;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// "Filter to X furni": the add-on that caps what the selectors handed the stack. It used to be
/// modelled as a selector reporting a code that already belonged to another box, so the client drew
/// the wrong dialog for it and it filtered nothing.
/// </summary>
public sealed class WiredSelectorFilterTests
{
    [Fact]
    public async Task TrimsThePoolToTheConfiguredAmount()
    {
        WiredSelectionSet pool = Pool(furni: [1, 2, 3, 4, 5]);
        TestFurniFilter box = new(StubContext(), Amount(2));

        await box.MutatePolicyAsync(Processing(pool), CancellationToken.None);

        pool.SelectedFurniIds.Should().HaveCount(2);
        pool.SelectedFurniIds.Should().BeSubsetOf([1, 2, 3, 4, 5]);
    }

    [Fact]
    public async Task PoolSmallerThanTheAmount_IsLeftAlone()
    {
        WiredSelectionSet pool = Pool(furni: [1, 2]);
        TestFurniFilter box = new(StubContext(), Amount(5));

        await box.MutatePolicyAsync(Processing(pool), CancellationToken.None);

        pool.SelectedFurniIds.Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public async Task TheFurniFilter_LeavesTheUserSideOfThePoolAlone()
    {
        WiredSelectionSet pool = Pool(furni: [1, 2, 3], players: [7, 8, 9]);
        TestFurniFilter box = new(StubContext(), Amount(1));

        await box.MutatePolicyAsync(Processing(pool), CancellationToken.None);

        pool.SelectedFurniIds.Should().HaveCount(1);
        pool.SelectedPlayerIds.Should().BeEquivalentTo([7, 8, 9]);
    }

    [Fact]
    public async Task TheUserFilter_TrimsTheUserSide()
    {
        WiredSelectionSet pool = Pool(furni: [1, 2, 3], players: [7, 8, 9]);
        TestUserFilter box = new(StubContext(), Amount(2));

        await box.MutatePolicyAsync(Processing(pool), CancellationToken.None);

        pool.SelectedPlayerIds.Should().HaveCount(2);
        pool.SelectedFurniIds.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public async Task AnUnconfiguredBox_LeavesThePoolAlone()
    {
        WiredSelectionSet pool = Pool(furni: [1, 2, 3]);
        TestFurniFilter box = new(StubContext(), new WiredData());

        await box.MutatePolicyAsync(Processing(pool), CancellationToken.None);

        pool.SelectedFurniIds.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public async Task AnAmountFromAVariableThatHoldsNothing_LeavesThePoolAlone()
    {
        // Treating the missing value as zero would empty the pool instead of doing nothing.
        WiredSelectionSet pool = Pool(furni: [1, 2, 3]);

        TestFurniFilter box = new(
            StubContext(),
            new WiredData
            {
                IntParams = [2, 1, (int)WiredVariableTargetType.Global],
                VariableIds = ["4242"],
            }
        );

        await box.MutatePolicyAsync(Processing(pool), CancellationToken.None);

        pool.SelectedFurniIds.Should().BeEquivalentTo([1, 2, 3]);
    }

    // ---- harness -------------------------------------------------------------------------------

    private static WiredData Amount(int amount) =>
        new() { IntParams = [amount, 0, (int)WiredVariableTargetType.Furni] };

    private static WiredSelectionSet Pool(int[]? furni = null, int[]? players = null)
    {
        WiredSelectionSet set = new();

        foreach (int id in furni ?? [])
        {
            set.SelectedFurniIds.Add(id);
        }

        foreach (int id in players ?? [])
        {
            set.SelectedPlayerIds.Add(id);
        }

        return set;
    }

    private static IWiredProcessingContext Processing(WiredSelectionSet pool) =>
        FakeProxy.Create<IWiredProcessingContext>(call =>
            call.Method.Name switch
            {
                "get_SelectorPool" => pool,
                "GetEffectiveSelectionAsync" => Task.FromResult<IWiredSelectionSet>(
                    new WiredSelectionSet()
                ),
                _ => null,
            }
        );

    private static IRoomFloorItemContext StubContext()
    {
        FurnitureDefinitionSnapshot definition = new()
        {
            Id = 1,
            SpriteId = 1,
            Name = "wf_xtra_filter_furni",
            ProductType = ProductType.Floor,
            FurniCategory = FurnitureCategory.Default,
            LogicName = "wf_xtra_filter_furni",
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

        // No variable resolves, which is what makes the "amount from a variable" case fail.
        IRoomFurniAccess furni = FakeProxy.Create<IRoomFurniAccess>(_ => null);

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

    private sealed class TestFurniFilter : WiredAddonFurniSelectorFilter
    {
        public TestFurniFilter(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx)
        {
            data.AttatchRules(GetIntParamRules());
            _wiredData = data;
        }
    }

    private sealed class TestUserFilter : WiredAddonUserSelectorFilter
    {
        public TestUserFilter(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx)
        {
            data.AttatchRules(GetIntParamRules());
            _wiredData = data;
        }
    }
}
