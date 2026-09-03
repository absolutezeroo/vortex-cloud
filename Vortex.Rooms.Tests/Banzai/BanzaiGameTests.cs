using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Games.Components;
using Vortex.Rooms.Games.BattleBanzai;
using Vortex.Rooms.Games.BattleBanzai.Components;
using Vortex.Rooms.Object.Avatars.Player;
using Vortex.Rooms.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Banzai;

/// <summary>
/// Battle Banzai end to end through the runtime: real components on real tiles, driven by the same
/// signals the furniture raises. What these cover that <c>BanzaiBoardTests</c> cannot is the wiring
/// — that a gate writes into the room's ONE shared team ledger (a second store was the original bug:
/// every wired team leaf reads the shared one, so a player who existed only in a private roster was
/// invisible to all of them), that a tile claim scores through the framework, and that a match ends
/// when its arena is dismantled underneath it.
/// </summary>
public sealed class BanzaiGameTests
{
    private static BanzaiGateComponent PlaceGate(
        RoomHarness harness,
        string colour,
        int x,
        int y
    ) =>
        GameFurni.Place(
            harness,
            $"battlebanzai_gate_{colour}",
            x,
            y,
            (factory, ctx) => new BanzaiGateComponent(factory, ctx)
        );

    private static BanzaiTileComponent PlaceTile(RoomHarness harness, int x, int y) =>
        GameFurni.Place(
            harness,
            "battlebanzai_tile",
            x,
            y,
            (factory, ctx) => new BanzaiTileComponent(factory, ctx)
        );

    [Fact]
    public async Task AGateTouch_JoinsTheSharedTeam_AndWearsTheWiredAura()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar avatar = harness.PutRealPlayerOnTile(RoomHarness.Stranger, 2, 2);
        BanzaiGateComponent gate = PlaceGate(harness, "red", 2, 2);

        await harness
            .Grain.GameRuntime.SignalAsync(
                GameSignal.WalkOn(gate, RoomHarness.Stranger),
                CancellationToken.None
            )
            .ConfigureAwait(true);

        harness.Grain.GameRuntime.GetTeam(RoomHarness.Stranger).Should().Be(GameTeamColor.Red);
        avatar.CurrentEffectId.Should().Be(33, "Banzai wears the wired aura set (32 + team)");
        gate.GetState().Should().Be(1, "the gate shows its team's member count");
    }

    [Fact]
    public async Task TouchingYourOwnGateAgain_LeavesTheTeamAndClearsTheAura()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar avatar = harness.PutRealPlayerOnTile(RoomHarness.Stranger, 2, 2);
        BanzaiGateComponent gate = PlaceGate(harness, "red", 2, 2);
        await WalkAsync(harness, gate).ConfigureAwait(true);

        await WalkAsync(harness, gate).ConfigureAwait(true);

        harness.Grain.GameRuntime.GetTeam(RoomHarness.Stranger).Should().Be(GameTeamColor.None);
        avatar.CurrentEffectId.Should().Be(0);
    }

    [Fact]
    public async Task SwitchingGates_MovesTheSharedMembership()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar avatar = harness.PutRealPlayerOnTile(RoomHarness.Stranger, 2, 2);
        BanzaiGateComponent red = PlaceGate(harness, "red", 2, 2);
        BanzaiGateComponent yellow = PlaceGate(harness, "yellow", 3, 2);
        await WalkAsync(harness, red).ConfigureAwait(true);

        await WalkAsync(harness, yellow).ConfigureAwait(true);

        harness.Grain.GameRuntime.GetTeam(RoomHarness.Stranger).Should().Be(GameTeamColor.Yellow);
        harness
            .Grain.GameRuntime.GetPlayersInTeam(GameTeamColor.Red)
            .Should()
            .BeEmpty("the old membership moved, it was not duplicated");
        avatar.CurrentEffectId.Should().Be(36);
    }

    [Fact]
    public async Task AMatchLightsTheArenaNeutral_AndClosesTheGates()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        harness.PutRealPlayerOnTile(RoomHarness.Stranger, 2, 2);
        BanzaiGateComponent gate = PlaceGate(harness, "red", 2, 2);
        BanzaiTileComponent tile = PlaceTile(harness, 5, 5);

        await harness.Grain.GameRuntime.StartGameAsync(CancellationToken.None).ConfigureAwait(true);

        harness.Grain.GameRuntime.PhaseOf(BanzaiConstants.Game).Should().Be(GamePhase.Running);
        tile.GetState().Should().Be(BanzaiConstants.TileNeutral);
        gate.CanWalk().Should().BeFalse("gates are shut for the duration of a match");
    }

    [Fact]
    public async Task ARoomWithNoArenaTiles_DoesNotStartAMatch()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        PlaceGate(harness, "red", 2, 2);

        await harness.Grain.GameRuntime.StartGameAsync(CancellationToken.None).ConfigureAwait(true);

        // A match on an arena that does not exist used to start, do nothing and say nothing.
        harness.Grain.GameRuntime.PhaseOf(BanzaiConstants.Game).Should().Be(GamePhase.Idle);
    }

    [Fact]
    public async Task ThreeStepsLockATile_AndTheLockIsWhatScores()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        harness.PutRealPlayerOnTile(RoomHarness.Stranger, 2, 2);
        BanzaiGateComponent gate = PlaceGate(harness, "red", 2, 2);
        BanzaiTileComponent tile = PlaceTile(harness, 5, 5);
        await WalkAsync(harness, gate).ConfigureAwait(true);
        await harness.Grain.GameRuntime.StartGameAsync(CancellationToken.None).ConfigureAwait(true);

        await WalkAsync(harness, tile).ConfigureAwait(true); // hijack the neutral tile
        harness.Grain.GameRuntime.GetTeamScore(GameTeamColor.Red).Should().Be(0);

        await WalkAsync(harness, tile).ConfigureAwait(true); // advance
        harness.Grain.GameRuntime.GetTeamScore(GameTeamColor.Red).Should().Be(0);

        await WalkAsync(harness, tile).ConfigureAwait(true); // lock

        tile.GetState().Should().Be(BanzaiBoard.LockedStateOf(GameTeamColor.Red));
        harness
            .Grain.GameRuntime.GetTeamScore(GameTeamColor.Red)
            .Should()
            .Be(
                BanzaiSettings.Default.PointsLockTile,
                "locking is the only scoring act by default"
            );
    }

    [Fact]
    public async Task ATeamlessWalker_ClaimsNothing()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        harness.PutRealPlayerOnTile(RoomHarness.Stranger, 2, 2);
        PlaceGate(harness, "red", 2, 2);
        BanzaiTileComponent tile = PlaceTile(harness, 5, 5);
        await harness.Grain.GameRuntime.StartGameAsync(CancellationToken.None).ConfigureAwait(true);

        await WalkAsync(harness, tile).ConfigureAwait(true);

        tile.GetState().Should().Be(BanzaiConstants.TileNeutral);
    }

    [Fact]
    public async Task StepsBeforeKickoff_DoNothing()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        harness.PutRealPlayerOnTile(RoomHarness.Stranger, 2, 2);
        BanzaiGateComponent gate = PlaceGate(harness, "red", 2, 2);
        BanzaiTileComponent tile = PlaceTile(harness, 5, 5);
        await WalkAsync(harness, gate).ConfigureAwait(true);

        await WalkAsync(harness, tile).ConfigureAwait(true);

        harness.Grain.GameRuntime.GetTeamScore(GameTeamColor.Red).Should().Be(0);
    }

    [Fact]
    public async Task TheLastArenaTileBeingPickedUp_EndsTheMatch()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        harness.PutRealPlayerOnTile(RoomHarness.Stranger, 2, 2);
        PlaceGate(harness, "red", 2, 2);
        BanzaiTileComponent tile = PlaceTile(harness, 5, 5);
        await harness.Grain.GameRuntime.StartGameAsync(CancellationToken.None).ConfigureAwait(true);

        await harness
            .Grain.GameRuntime.SignalAsync(GameSignal.Detached(tile), CancellationToken.None)
            .ConfigureAwait(true);

        // A match on a board that no longer exists is a match nobody can end; the arena going away
        // has to end it.
        harness.Grain.GameRuntime.PhaseOf(BanzaiConstants.Game).Should().NotBe(GamePhase.Running);
    }

    private static Task WalkAsync(RoomHarness harness, IGameComponent component) =>
        harness.Grain.GameRuntime.SignalAsync(
            GameSignal.WalkOn(component, RoomHarness.Stranger),
            CancellationToken.None
        );
}
