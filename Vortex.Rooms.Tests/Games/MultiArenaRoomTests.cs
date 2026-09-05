using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Object;
using Vortex.Rooms.Games.BattleBanzai;
using Vortex.Rooms.Games.BattleBanzai.Components;
using Vortex.Rooms.Games.Football;
using Vortex.Rooms.Games.Football.Components;
using Vortex.Rooms.Games.Runtime;
using Vortex.Rooms.Object.Logic.Furniture.Floor;
using Vortex.Rooms.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Games;

/// <summary>
/// A room holding more than one playable game, driven through the real runtime and the real
/// furniture. This is the scenario the old architecture got wrong: a hall with a Banzai board AND a
/// football pitch answered a single press of a single counter by starting BOTH matches.
/// </summary>
public sealed class MultiArenaRoomTests
{
    private static BanzaiTileComponent PlaceBanzaiTile(RoomHarness harness, int x, int y) =>
        GameFurni.Place(
            harness,
            "battlebanzai_tile",
            x,
            y,
            (factory, ctx) => new BanzaiTileComponent(factory, ctx)
        );

    private static FootballBallComponent PlaceBall(RoomHarness harness, int x, int y) =>
        GameFurni.Place(
            harness,
            "furniture_pushable",
            x,
            y,
            (factory, ctx) => new FootballBallComponent(factory, ctx)
        );

    private static FootballGoalComponent PlaceGoal(
        RoomHarness harness,
        string colour,
        int x,
        int y
    ) =>
        GameFurni.Place(
            harness,
            $"football_goal_{colour}",
            x,
            y,
            (factory, ctx) => new FootballGoalComponent(factory, ctx),
            rotation: Rotation.West
        );

    /// <summary>Builds a room with a Banzai board in one corner and a football pitch in the other.</summary>
    private static void BuildHall(RoomHarness harness)
    {
        PlaceBanzaiTile(harness, 1, 1);
        PlaceBanzaiTile(harness, 2, 1);

        PlaceBall(harness, 9, 9);
        PlaceGoal(harness, "red", 10, 9);
        PlaceGoal(harness, "blue", 8, 9);
    }

    private static Task<bool> StartAsync(RoomHarness harness, RoomObjectId source = default) =>
        harness.Grain.GameRuntime.StartGameAsync(source, GameId.None, CancellationToken.None);

    [Fact]
    public async Task AHallWithTwoDifferentGames_StartsNeitherOnABareRequest()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        BuildHall(harness);

        bool started = await StartAsync(harness).ConfigureAwait(true);

        // The regression, stated plainly: one request, two valid arenas, nothing to choose between
        // them. It used to start both.
        started.Should().BeFalse();
        harness.Grain.GameRuntime.PhaseOf(BanzaiConstants.Game).Should().Be(GamePhase.Idle);
        harness.Grain.GameRuntime.PhaseOf(FootballConstants.Game).Should().Be(GamePhase.Idle);

        // The room's round still opens — it is a room-level wired concept, not a match — and the
        // point is that it opened with NO match under it.
        harness.RoomEvents.OfType<WiredGameStartedEvent>().Should().ContainSingle();
    }

    [Fact]
    public async Task ACounterBesideTheBanzaiBoard_StartsBanzaiAndOnlyBanzai()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        BuildHall(harness);
        FurnitureGameTimerLogic counter = PlaceCounter(harness, 2, 2);

        bool started = await StartAsync(harness, counter.Context.ObjectId).ConfigureAwait(true);

        started.Should().BeTrue();
        harness.Grain.GameRuntime.PhaseOf(BanzaiConstants.Game).Should().Be(GamePhase.Running);
        harness
            .Grain.GameRuntime.PhaseOf(FootballConstants.Game)
            .Should()
            .Be(GamePhase.Idle, "the pitch is at the other end of the hall");
    }

    [Fact]
    public async Task ACounterBesideThePitch_StartsFootballAndOnlyFootball()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        BuildHall(harness);
        FurnitureGameTimerLogic counter = PlaceCounter(harness, 9, 8);

        await StartAsync(harness, counter.Context.ObjectId).ConfigureAwait(true);

        harness.Grain.GameRuntime.PhaseOf(FootballConstants.Game).Should().Be(GamePhase.Running);
        harness.Grain.GameRuntime.PhaseOf(BanzaiConstants.Game).Should().Be(GamePhase.Idle);
    }

    [Fact]
    public async Task StoppingFromOneCounter_LeavesTheOtherMatchRunning()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        BuildHall(harness);
        FurnitureGameTimerLogic banzaiCounter = PlaceCounter(harness, 2, 2);
        FurnitureGameTimerLogic pitchCounter = PlaceCounter(harness, 9, 8);
        await StartAsync(harness, banzaiCounter.Context.ObjectId).ConfigureAwait(true);
        await StartAsync(harness, pitchCounter.Context.ObjectId).ConfigureAwait(true);

        await harness
            .Grain.GameRuntime.EndGameAsync(
                pitchCounter.Context.ObjectId,
                GameId.None,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        harness.Grain.GameRuntime.PhaseOf(FootballConstants.Game).Should().Be(GamePhase.Idle);
        harness
            .Grain.GameRuntime.PhaseOf(BanzaiConstants.Game)
            .Should()
            .Be(GamePhase.Running, "stopping one match must not reach across the room");
    }

    [Fact]
    public async Task TheRoomsRound_SpansEveryArena()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        BuildHall(harness);
        FurnitureGameTimerLogic banzaiCounter = PlaceCounter(harness, 2, 2);
        FurnitureGameTimerLogic pitchCounter = PlaceCounter(harness, 9, 8);

        await StartAsync(harness, banzaiCounter.Context.ObjectId).ConfigureAwait(true);
        await StartAsync(harness, pitchCounter.Context.ObjectId).ConfigureAwait(true);

        // GAME_STARTS / GAME_ENDS are room-level wired triggers. Two matches are still one round.
        harness.RoomEvents.OfType<WiredGameStartedEvent>().Should().ContainSingle();

        await EndAsync(harness, banzaiCounter).ConfigureAwait(true);
        harness
            .RoomEvents.OfType<WiredGameEndedEvent>()
            .Should()
            .BeEmpty("one arena is still playing");

        await EndAsync(harness, pitchCounter).ConfigureAwait(true);
        harness.RoomEvents.OfType<WiredGameEndedEvent>().Should().ContainSingle();
    }

    [Fact]
    public async Task AMatchOnOneArena_HasAMatchIdTheOtherCannotShare()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        BuildHall(harness);
        FurnitureGameTimerLogic banzaiCounter = PlaceCounter(harness, 2, 2);
        FurnitureGameTimerLogic pitchCounter = PlaceCounter(harness, 9, 8);

        await StartAsync(harness, banzaiCounter.Context.ObjectId).ConfigureAwait(true);
        await StartAsync(harness, pitchCounter.Context.ObjectId).ConfigureAwait(true);

        // Deferred work carries its match id and drops itself when it no longer matches. Two live
        // matches in one room must therefore be distinguishable, arena included.
        ArenaId banzai = new(BanzaiConstants.Game, 0);
        ArenaId football = new(FootballConstants.Game, 0);

        harness.Grain.GameRuntime.Arenas.Should().Contain([banzai, football]);
        new MatchId(harness.Grain.RoomId, banzai, 1)
            .Should()
            .NotBe(new MatchId(harness.Grain.RoomId, football, 1));
    }

    [Fact]
    public async Task ARoomWithOneValidGameAmongMany_StillStartsFromABareCounter()
    {
        // The ordinary room, and the reason the resolver's last rule exists: Banzai, Freeze and
        // football are all hosted, but only one of them has an arena, so there is nothing to confuse.
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        PlaceBanzaiTile(harness, 1, 1);
        PlaceBanzaiTile(harness, 2, 1);

        bool started = await StartAsync(harness).ConfigureAwait(true);

        started.Should().BeTrue();
        harness.Grain.GameRuntime.PhaseOf(BanzaiConstants.Game).Should().Be(GamePhase.Running);
    }

    private static Task EndAsync(RoomHarness harness, FurnitureGameTimerLogic counter) =>
        harness.Grain.GameRuntime.EndGameAsync(
            counter.Context.ObjectId,
            GameId.None,
            CancellationToken.None
        );

    private static FurnitureGameTimerLogic PlaceCounter(RoomHarness harness, int x, int y) =>
        GameFurni.Place(
            harness,
            "game_timer",
            x,
            y,
            (factory, ctx) => new FurnitureGameTimerLogic(factory, ctx)
        );
}
