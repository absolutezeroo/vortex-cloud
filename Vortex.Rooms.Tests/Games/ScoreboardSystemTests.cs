using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Furniture;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Furniture;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Games;
using Vortex.Rooms.Tests.Support;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Games;

/// <summary>
/// The event-driven scoreboard brick. Freeze used to repaint its own boards by hand at four call
/// sites, which meant a wired give-score box changed the shared score and no board moved; the brick
/// listens to the same events the wired boxes read, so ANY score change paints the matching colour's
/// boards — during a round or not — and GAME_STARTS / GAME_ENDS repaint everything. GAME_ENDS also
/// hands the round result to every high-score board while the teams are still standing.
/// </summary>
public sealed class ScoreboardSystemTests
{
    [Fact]
    public async Task AScoreChange_PaintsOnlyTheMatchingColoursBoards()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FurnitureScoreboardLogic red = AttachScoreboard(
            harness,
            "es_score_r",
            "freeze_counter_red"
        );
        FurnitureScoreboardLogic blue = AttachScoreboard(harness, "bb_score_b", "furniture_score");

        await harness
            .Grain.GameSystem.AddTeamScoreAsync(GameTeamColor.Red, 5, CancellationToken.None)
            .ConfigureAwait(true);

        red.GetState().Should().Be(5);
        blue.GetState().Should().Be(0);
    }

    [Fact]
    public async Task AWiredGiveScore_OutsideAnyRound_StillPaintsTheBoards()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FurnitureScoreboardLogic red = AttachScoreboard(
            harness,
            "es_score_r",
            "freeze_counter_red"
        );
        harness.PutRealPlayerInRoom(RoomHarness.Stranger, 2, 2);
        await harness
            .Grain.GameSystem.JoinTeamAsync(
                RoomHarness.Stranger,
                GameTeamColor.Red,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        // The capped wired give-score path, with no round running — the boards must live-update,
        // which is what they do on Habbo.
        bool given = await harness
            .Grain.GameSystem.TryGiveScoreToPlayerTeamAsync(
                new RoomObjectId(9000),
                RoomHarness.Stranger,
                3,
                0,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        given.Should().BeTrue();
        red.GetState().Should().Be(3);
    }

    [Fact]
    public async Task ARoundStart_ZeroesTheBoards()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FurnitureScoreboardLogic red = AttachScoreboard(
            harness,
            "es_score_r",
            "freeze_counter_red"
        );
        await harness
            .Grain.GameSystem.AddTeamScoreAsync(GameTeamColor.Red, 7, CancellationToken.None)
            .ConfigureAwait(true);

        await harness.Grain.GameSystem.StartGameAsync(CancellationToken.None).ConfigureAwait(true);

        // The coordinator reset the shared scores before announcing the start; the boards must
        // follow, or last round's tally sits there through the whole next round.
        red.GetState().Should().Be(0);
    }

    [Fact]
    public async Task ARoundEnd_LeavesTheFinalTallyOnTheBoards()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FurnitureScoreboardLogic red = AttachScoreboard(
            harness,
            "es_score_r",
            "freeze_counter_red"
        );
        await harness.Grain.GameSystem.StartGameAsync(CancellationToken.None).ConfigureAwait(true);
        await harness
            .Grain.GameSystem.AddTeamScoreAsync(GameTeamColor.Red, 4, CancellationToken.None)
            .ConfigureAwait(true);

        await harness.Grain.GameSystem.EndGameAsync(CancellationToken.None).ConfigureAwait(true);

        red.GetState().Should().Be(4);
    }

    [Fact]
    public async Task ARoundEnd_RecordsTheResultOnEveryHighScoreBoard()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FurnitureHighScoreLogic board = await AttachHighScoreAsync(harness, "highscore_classic*1")
            .ConfigureAwait(true);
        harness.PutRealPlayerInRoom(RoomHarness.Stranger, 2, 2);
        await harness
            .Grain.GameSystem.JoinTeamAsync(
                RoomHarness.Stranger,
                GameTeamColor.Red,
                CancellationToken.None
            )
            .ConfigureAwait(true);
        await harness.Grain.GameSystem.StartGameAsync(CancellationToken.None).ConfigureAwait(true);
        await harness
            .Grain.GameSystem.AddTeamScoreAsync(GameTeamColor.Red, 10, CancellationToken.None)
            .ConfigureAwait(true);

        await harness.Grain.GameSystem.EndGameAsync(CancellationToken.None).ConfigureAwait(true);

        IHighscoreStuffData data = (IHighscoreStuffData)board.StuffData;
        HighscoreEntry entry = data.Entries.Should().ContainSingle().Subject;
        entry.Score.Should().Be(10);
        entry.Win.Should().BeTrue();
        data.HighscoreData.Should().ContainKey(10);
    }

    [Fact]
    public async Task AScorelessRound_RecordsNothing()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FurnitureHighScoreLogic board = await AttachHighScoreAsync(harness, "highscore_classic*1")
            .ConfigureAwait(true);
        await harness.Grain.GameSystem.StartGameAsync(CancellationToken.None).ConfigureAwait(true);

        await harness.Grain.GameSystem.EndGameAsync(CancellationToken.None).ConfigureAwait(true);

        ((IHighscoreStuffData)board.StuffData).Entries.Should().BeEmpty();
    }

    // ---- builders -----------------------------------------------------------

    private static FurnitureScoreboardLogic AttachScoreboard(
        RoomHarness harness,
        string classname,
        string logicName
    )
    {
        (IRoomItem item, FurnitureScoreboardLogic logic) = BuildItem(
            classname,
            logicName,
            StuffDataType.LegacyKey,
            (factory, ctx) => new FurnitureScoreboardLogic(factory, ctx)
        );

        harness.Grain._state.ItemIndex.OnLogicAttached(item);

        return logic;
    }

    private static async Task<FurnitureHighScoreLogic> AttachHighScoreAsync(
        RoomHarness harness,
        string classname
    )
    {
        (IRoomItem item, FurnitureHighScoreLogic logic) = BuildItem(
            classname,
            "wf_highscore",
            StuffDataType.HighscoreKey,
            (factory, ctx) => new FurnitureHighScoreLogic(factory, ctx)
        );

        await logic.OnAttachAsync(CancellationToken.None).ConfigureAwait(true);
        harness.Grain._state.ItemIndex.OnLogicAttached(item);

        return logic;
    }

    private static (IRoomItem Item, TLogic Logic) BuildItem<TLogic>(
        string classname,
        string logicName,
        StuffDataType stuffDataType,
        System.Func<StuffDataFactory, IRoomFloorItemContext, TLogic> create
    )
        where TLogic : class
    {
        FurnitureDefinitionSnapshot definition = new()
        {
            Id = 1,
            SpriteId = 1,
            Name = classname,
            ProductType = ProductType.Floor,
            FurniCategory = FurnitureCategory.Default,
            LogicName = logicName,
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
            StuffDataType = stuffDataType,
        };

        IExtraData extraData = new ExtraData(null);
        object? logicRef = null;

        IRoomFloorItem item = FakeProxy.Create<IRoomFloorItem>(call =>
            call.Method.Name switch
            {
                "get_ExtraData" => extraData,
                "get_Definition" => definition,
                "get_Logic" => logicRef,
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

        TLogic logic = create(new StuffDataFactory(), ctx);
        logicRef = logic;

        return (item, logic);
    }
}
