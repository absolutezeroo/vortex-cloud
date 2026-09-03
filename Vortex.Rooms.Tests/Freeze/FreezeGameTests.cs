using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Games.Components;
using Vortex.Rooms.Games.Freeze;
using Vortex.Rooms.Games.Freeze.Components;
using Vortex.Rooms.Object.Avatars.Player;
using Vortex.Rooms.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Freeze;

/// <summary>
/// Freeze end to end through the runtime: gates, the throw pipeline, the blast landing on a delay,
/// and the ice blocks. What this covers beyond the pure roster tests is the wiring — that a throw is
/// an intent the server decides on, that the blast lands a match later and not in the next one, and
/// that a match ending thaws everybody rather than leaving a movement lock behind.
/// </summary>
public sealed class FreezeGameTests
{
    private const long Kickoff = 10_000;

    private static FreezeGateComponent PlaceGate(
        RoomHarness harness,
        string colour,
        int x,
        int y
    ) =>
        GameFurni.Place(
            harness,
            $"freeze_gate_{colour}",
            x,
            y,
            (factory, ctx) => new FreezeGateComponent(factory, ctx)
        );

    private static FreezeTileComponent PlaceTile(RoomHarness harness, int x, int y) =>
        GameFurni.Place(
            harness,
            "freeze_tile",
            x,
            y,
            (factory, ctx) => new FreezeTileComponent(factory, ctx)
        );

    private static FreezeBlockComponent PlaceBlock(RoomHarness harness, int x, int y) =>
        GameFurni.Place(
            harness,
            "freeze_block",
            x,
            y,
            (factory, ctx) => new FreezeBlockComponent(factory, ctx)
        );

    [Fact]
    public async Task AGateTouch_JoinsTheSharedTeam_AndWearsTheFreezeAura()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar avatar = harness.PutRealPlayerOnTile(RoomHarness.Stranger, 2, 2);
        FreezeGateComponent gate = PlaceGate(harness, "red", 2, 2);

        await Walk(harness, gate).ConfigureAwait(true);

        harness.Grain.GameRuntime.GetTeam(RoomHarness.Stranger).Should().Be(GameTeamColor.Red);
        avatar
            .CurrentEffectId.Should()
            .Be(40, "Freeze wears its own aura set (39 + team), not the wired one");
        gate.GetState().Should().Be(1);
    }

    [Fact]
    public async Task ARoomWithNoArenaTiles_DoesNotStartAMatch()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        PlaceGate(harness, "red", 2, 2);

        await harness.Grain.GameRuntime.StartGameAsync(CancellationToken.None).ConfigureAwait(true);

        harness.Grain.GameRuntime.PhaseOf(FreezeConstants.Game).Should().Be(GamePhase.Idle);
    }

    [Fact]
    public async Task AThrowRaisesTheTile_AndTheBlastLandsOnItsOwnDelay()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        harness.PutRealPlayerOnTile(RoomHarness.Stranger, 5, 5);
        FreezeGateComponent gate = PlaceGate(harness, "red", 2, 2);
        FreezeTileComponent tile = PlaceTile(harness, 5, 5);
        await Walk(harness, gate).ConfigureAwait(true);
        await StartAsync(harness).ConfigureAwait(true);

        await Use(harness, tile).ConfigureAwait(true);

        tile.GetState().Should().NotBe(FreezeConstants.TileIdle, "the ball rises before it lands");

        await harness
            .Grain.GameRuntime.TickAsync(
                Kickoff + FreezeConstants.BlastDelayMs,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        tile.GetState().Should().Be(FreezeConstants.TileBlast * FreezeConstants.StateWireScale);
    }

    [Fact]
    public async Task AThrowAtATileTooFarAway_IsRefused()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        harness.PutRealPlayerOnTile(RoomHarness.Stranger, 1, 1);
        FreezeGateComponent gate = PlaceGate(harness, "red", 2, 2);
        FreezeTileComponent far = PlaceTile(harness, 8, 8);
        await Walk(harness, gate).ConfigureAwait(true);
        await StartAsync(harness).ConfigureAwait(true);

        await Use(harness, far).ConfigureAwait(true);

        // The client sends intent; the server decides. A throw at the other end of the room is a
        // client that made something up.
        far.GetState().Should().Be(FreezeConstants.TileIdle);
    }

    [Fact]
    public async Task AThrowBeforeKickoff_IsRefused()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        harness.PutRealPlayerOnTile(RoomHarness.Stranger, 5, 5);
        FreezeGateComponent gate = PlaceGate(harness, "red", 2, 2);
        FreezeTileComponent tile = PlaceTile(harness, 5, 5);
        await Walk(harness, gate).ConfigureAwait(true);

        await Use(harness, tile).ConfigureAwait(true);

        tile.GetState().Should().Be(FreezeConstants.TileIdle);
    }

    [Fact]
    public async Task ABlastFreezesAnEnemyOnTheTile_AndScoresTheThrower()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        // Diagonally apart, and the throw is diagonal too. That is not decoration: a default blast
        // reaches one tile along each CARDINAL arm, so a thrower who aims at the tile beside them is
        // inside their own blast and freezes themselves. Aiming diagonally is what keeps them out.
        RoomPlayerAvatar thrower = harness.PutRealPlayerOnTile(RoomHarness.Stranger, 5, 5);
        RoomPlayerAvatar victim = harness.PutRealPlayerOnTile(RoomHarness.Owner, 6, 6);
        FreezeGateComponent red = PlaceGate(harness, "red", 2, 2);
        FreezeGateComponent blue = PlaceGate(harness, "blue", 3, 2);
        FreezeTileComponent target = PlaceTile(harness, 6, 6);

        await harness
            .Grain.GameRuntime.SignalAsync(
                GameSignal.WalkOn(red, RoomHarness.Stranger),
                CancellationToken.None
            )
            .ConfigureAwait(true);
        await harness
            .Grain.GameRuntime.SignalAsync(
                GameSignal.WalkOn(blue, RoomHarness.Owner),
                CancellationToken.None
            )
            .ConfigureAwait(true);
        await StartAsync(harness).ConfigureAwait(true);

        await Use(harness, target).ConfigureAwait(true);
        await harness
            .Grain.GameRuntime.TickAsync(
                Kickoff + FreezeConstants.BlastDelayMs,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        victim.CurrentEffectId.Should().Be(FreezeConstants.FrozenEffect);
        thrower.CurrentEffectId.Should().Be(40, "the thrower keeps their team aura");
        harness
            .Grain.GameRuntime.GetTeamScore(GameTeamColor.Red)
            .Should()
            .Be(FreezeSettings.Default.FreezePlayerPoints);
    }

    [Fact]
    public async Task AMatchEnding_ThawsAndUnlocksEveryone()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar player = harness.PutRealPlayerOnTile(RoomHarness.Stranger, 5, 5);
        FreezeGateComponent gate = PlaceGate(harness, "red", 2, 2);
        PlaceTile(harness, 5, 5);
        await Walk(harness, gate).ConfigureAwait(true);
        await StartAsync(harness).ConfigureAwait(true);

        await harness.Grain.GameRuntime.EndGameAsync(CancellationToken.None).ConfigureAwait(true);

        // A lock that outlived the match would strand a player until a wired unfreeze box happened
        // to fire.
        player.CurrentEffectId.Should().Be(0);
        player.IsMovementLocked.Should().BeFalse();
    }

    [Fact]
    public async Task IceBlocksAreRestoredToIntact_WhenAMatchIsPrepared()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        harness.PutRealPlayerOnTile(RoomHarness.Stranger, 5, 5);
        PlaceGate(harness, "red", 2, 2);
        PlaceTile(harness, 5, 5);
        FreezeBlockComponent block = PlaceBlock(harness, 6, 6);
        await block.SetStateAsync(FreezeConstants.BlockEmpty).ConfigureAwait(true);

        await StartAsync(harness).ConfigureAwait(true);

        // Restoring the arena is the framework's cleanup contract, not something a game remembers.
        block.GetState().Should().Be(FreezeConstants.BlockIntact);
    }

    private static async Task StartAsync(RoomHarness harness)
    {
        // One tick first so the runtime has a clock: a throw queued at "now" needs one.
        await harness
            .Grain.GameRuntime.TickAsync(Kickoff, CancellationToken.None)
            .ConfigureAwait(true);
        await harness.Grain.GameRuntime.StartGameAsync(CancellationToken.None).ConfigureAwait(true);
    }

    private static Task Walk(RoomHarness harness, IGameComponent component) =>
        harness.Grain.GameRuntime.SignalAsync(
            GameSignal.WalkOn(component, RoomHarness.Stranger),
            CancellationToken.None
        );

    private static Task Use(RoomHarness harness, IGameComponent component) =>
        harness.Grain.GameRuntime.SignalAsync(
            GameSignal.Use(component, RoomHarness.Stranger, 0),
            CancellationToken.None
        );
}
