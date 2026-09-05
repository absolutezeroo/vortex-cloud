using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Rooms.Enums;
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

        await harness
            .Grain.GameRuntime.StartGameAsync(default, GameId.None, CancellationToken.None)
            .ConfigureAwait(true);

        harness.Grain.GameRuntime.PhaseOf(BanzaiConstants.Game).Should().Be(GamePhase.Running);
        tile.GetState().Should().Be(BanzaiConstants.TileNeutral);
        gate.CanWalk().Should().BeFalse("gates are shut for the duration of a match");
    }

    [Fact]
    public async Task ARoomWithNoArenaTiles_DoesNotStartAMatch()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        PlaceGate(harness, "red", 2, 2);

        await harness
            .Grain.GameRuntime.StartGameAsync(default, GameId.None, CancellationToken.None)
            .ConfigureAwait(true);

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
        await harness
            .Grain.GameRuntime.StartGameAsync(default, GameId.None, CancellationToken.None)
            .ConfigureAwait(true);

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
        await harness
            .Grain.GameRuntime.StartGameAsync(default, GameId.None, CancellationToken.None)
            .ConfigureAwait(true);

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
        await harness
            .Grain.GameRuntime.StartGameAsync(default, GameId.None, CancellationToken.None)
            .ConfigureAwait(true);

        await harness
            .Grain.GameRuntime.SignalAsync(GameSignal.Detached(tile), CancellationToken.None)
            .ConfigureAwait(true);

        // A match on a board that no longer exists is a match nobody can end; the arena going away
        // has to end it.
        harness.Grain.GameRuntime.PhaseOf(BanzaiConstants.Game).Should().NotBe(GamePhase.Running);
    }

    [Fact]
    public async Task ARandomTeleport_CancelsTheWalkItInterrupted()
    {
        // Stepping on a teleporter cancels the walk that brought you there — but the hop only fires
        // TeleportDelayMs later, and nothing stops the player walking off again in between. THAT
        // walk is the one the hop used to leave in place: its path was routed from the tile the
        // avatar is taken off, so the next tick walked them straight back, move status still on.
        // On screen: it teleports, undoes itself, and the avatar keeps its walk animation.
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        BanzaiTeleportComponent source = PlaceTeleport(harness, 3, 3, chains: true);
        PlaceTeleport(harness, 8, 8, chains: false);

        RoomPlayerAvatar player = harness.PutRealPlayerOnTile(RoomHarness.Stranger, 3, 3);

        await WalkAsync(harness, source).ConfigureAwait(true);

        // Inside the 500ms window: they set off again.
        int oldGoal = harness.Grain.MapModule.ToIdx(3, 9);
        player.IsWalking = true;
        player.SetGoalTileId(oldGoal);
        player.TilePath.Add(harness.Grain.MapModule.ToIdx(3, 4));
        player.AddStatus(AvatarStatusType.Move, "3,4,0");

        // The setup is half the test: if the avatar is not actually mid-walk when the hop fires,
        // every assertion below passes on state that was never there.
        player.TilePath.Should().ContainSingle();
        player.GoalTileId.Should().Be(oldGoal);
        player.Statuses.Should().ContainKey(AvatarStatusType.Move);

        await harness
            .Grain.GameRuntime.TickAsync(
                BanzaiConstants.TeleportDelayMs + 100,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        (player.X, player.Y).Should().Be((8, 8), "the hop lands on the other teleporter");

        player.IsWalking.Should().BeFalse();
        player.TilePath.Should().BeEmpty("a path routed from the old tile is stale");
        player.GoalTileId.Should().Be(-1);
        player
            .Statuses.Should()
            .NotContainKey(AvatarStatusType.Move, "or the avatar keeps its walk animation");
    }

    private static BanzaiTeleportComponent PlaceTeleport(
        RoomHarness harness,
        int x,
        int y,
        bool chains
    ) =>
        GameFurni.Place(
            harness,
            chains ? "battlebanzai_random_teleport" : "battlebanzai_random_teleport_exclude",
            x,
            y,
            (factory, ctx) => new BanzaiTeleportComponent(factory, ctx)
        );

    private static Task WalkAsync(RoomHarness harness, IGameComponent component) =>
        harness.Grain.GameRuntime.SignalAsync(
            GameSignal.WalkOn(component, RoomHarness.Stranger),
            CancellationToken.None
        );
}
