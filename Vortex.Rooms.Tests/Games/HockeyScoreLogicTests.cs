using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Furniture;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Games;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Games;

/// <summary>
/// The hand-operated match counter (<c>hockey_score</c> / <c>fball_score_*</c>). The referee clicks
/// sprite regions the client sends as use params — inc=2, dec=1, reset=3 — and the raw state is the
/// displayed score. The mapping is easy to get backwards (dec is the LOWER param), which would make
/// every + click subtract; these tests pin it.
/// </summary>
public sealed class HockeyScoreLogicTests
{
    [Fact]
    public async Task Increment_Decrement_And_Reset_DriveTheDisplayedScore()
    {
        FurnitureHockeyScoreLogic score = Build();

        await score.OnUseAsync(ActionContext.System, 2, CancellationToken.None);
        await score.OnUseAsync(ActionContext.System, 2, CancellationToken.None);
        score.GetState().Should().Be(2);

        await score.OnUseAsync(ActionContext.System, 1, CancellationToken.None);
        score.GetState().Should().Be(1);

        await score.OnUseAsync(ActionContext.System, 3, CancellationToken.None);
        score.GetState().Should().Be(0);
    }

    [Fact]
    public async Task Decrement_FloorsAtZero()
    {
        FurnitureHockeyScoreLogic score = Build();

        await score.OnUseAsync(ActionContext.System, 1, CancellationToken.None);

        score.GetState().Should().Be(0);
    }

    [Fact]
    public async Task AnUnknownParam_ChangesNothing()
    {
        FurnitureHockeyScoreLogic score = Build();

        await score.OnUseAsync(ActionContext.System, 2, CancellationToken.None);
        await score.OnUseAsync(ActionContext.System, 0, CancellationToken.None);

        score.GetState().Should().Be(1);
    }

    [Fact]
    public void TheCounter_IsControllerOnly_AndNeverPersisted()
    {
        FurnitureHockeyScoreLogic score = Build();

        // A match counter anyone could click is a griefing tool, and a persisted one would reload
        // rooms mid-"match".
        score.GetUsagePolicy().Should().Be(FurnitureUsageType.Controller);
    }

    private static FurnitureHockeyScoreLogic Build()
    {
        FurnitureDefinitionSnapshot definition = new()
        {
            Id = 1,
            SpriteId = 1,
            Name = "hockey_score",
            ProductType = ProductType.Floor,
            FurniCategory = FurnitureCategory.Default,
            LogicName = "furniture_hockey_score",
            TotalStates = 100,
            Width = 1,
            Length = 1,
            StackHeight = default,
            CanStack = false,
            CanWalk = false,
            CanSit = false,
            CanLay = false,
            CanRecycle = false,
            CanTrade = true,
            CanGroup = false,
            CanSell = true,
            UsagePolicy = FurnitureUsageType.Controller,
            ExtraData = null,
            StuffDataType = StuffDataType.LegacyKey,
        };

        IExtraData extraData = new ExtraData(null);

        IRoomFloorItem item = FakeProxy.Create<IRoomFloorItem>(call =>
            call.Method.Name switch
            {
                "get_ExtraData" => extraData,
                "get_Definition" => definition,
                _ => null,
            }
        );

        IRoomFloorItemContext ctx = FakeProxy.Create<IRoomFloorItemContext>(call =>
            call.Method.Name switch
            {
                "get_Definition" => definition,
                "get_RoomObject" => item,
                _ => null,
            }
        );

        return new FurnitureHockeyScoreLogic(new StuffDataFactory(), ctx);
    }
}
