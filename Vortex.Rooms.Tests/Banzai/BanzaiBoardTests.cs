using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Rooms.Games.BattleBanzai;
using Xunit;

namespace Vortex.Rooms.Tests.Banzai;

/// <summary>
/// The Battle Banzai claim state machine and enclosure rule, pinned against Arcturus
/// (<c>InteractionBattleBanzaiTile</c> / <c>BattleBanzaiGame</c>): team t claims through t*3 →
/// t*3+1 → t*3+2 (locked); locked is inert; anything else hijacks to the stepper's first claim.
/// Locking flood-fills from the tile's neighbours — a pocket bounded entirely by your locked tiles
/// locks wholesale, a region touching any non-board position leaks and locks nothing, and (the
/// Arcturus quirk, deliberately mirrored and pinned here) only the LARGEST surviving pocket locks.
/// The board runs on tile indices over the room grid, so the tests build tiny explicit grids.
/// </summary>
public sealed class BanzaiBoardTests
{
    private const int Width = 10;

    private static int Idx(int x, int y) => y * Width + x;

    private static BanzaiBoard Board(params (int X, int Y)[] tiles)
    {
        BanzaiBoard board = new();
        board.Activate(tiles.Select(t => Idx(t.X, t.Y)), Width);

        return board;
    }

    [Fact]
    public void AClaim_AdvancesThroughThreeSteps_ThenLocks()
    {
        BanzaiBoard board = Board((1, 1));
        int tile = Idx(1, 1);

        BanzaiMarkResult first = board.Mark(GameTeamColor.Red, tile);
        first.Kind.Should().Be(BanzaiMarkKind.Hijack, "a neutral tile is taken, not advanced");
        first.NewState.Should().Be(3);

        BanzaiMarkResult second = board.Mark(GameTeamColor.Red, tile);
        second.Kind.Should().Be(BanzaiMarkKind.Fill);
        second.NewState.Should().Be(4);

        BanzaiMarkResult third = board.Mark(GameTeamColor.Red, tile);
        third.Kind.Should().Be(BanzaiMarkKind.Lock);
        third.NewState.Should().Be(5);

        board
            .Mark(GameTeamColor.Red, tile)
            .Kind.Should()
            .Be(BanzaiMarkKind.None, "locked is inert");
    }

    [Fact]
    public void EveryTeam_UsesItsOwnStateRange()
    {
        // Red 3/4/5, Green 6/7/8, Blue 9/10/11, Yellow 12/13/14 — the client's visualization maps
        // these to colours, so one off-by-three shows the wrong team's colour on the wire.
        BanzaiBoard.FirstClaimStateOf(GameTeamColor.Red).Should().Be(3);
        BanzaiBoard.LockedStateOf(GameTeamColor.Red).Should().Be(5);
        BanzaiBoard.FirstClaimStateOf(GameTeamColor.Green).Should().Be(6);
        BanzaiBoard.FirstClaimStateOf(GameTeamColor.Blue).Should().Be(9);
        BanzaiBoard.LockedStateOf(GameTeamColor.Yellow).Should().Be(14);
    }

    [Fact]
    public void AnEnemyClaim_IsHijackedBackToFirstStep()
    {
        BanzaiBoard board = Board((1, 1));
        int tile = Idx(1, 1);

        board.Mark(GameTeamColor.Red, tile);
        board.Mark(GameTeamColor.Red, tile); // red second claim (4)

        BanzaiMarkResult stolen = board.Mark(GameTeamColor.Blue, tile);

        stolen.Kind.Should().Be(BanzaiMarkKind.Hijack);
        stolen.NewState.Should().Be(9, "blue's FIRST claim — hijacking never inherits progress");
    }

    [Fact]
    public void AnEnclosedPocket_LocksWithTheRing()
    {
        // A 3x3 ring of red-locked tiles around (2,2): locking the last ring tile must swallow the
        // centre. The last one is a CARDINAL neighbour of the pocket — the fill is 4-neighbour, so
        // the pocket closes the moment its fourth cardinal locks, corners never matter.
        (int X, int Y)[] ring = [(1, 1), (2, 1), (3, 1), (1, 2), (3, 2), (1, 3), (3, 3), (2, 3)];
        BanzaiBoard board = Board([.. ring, (2, 2)]);

        // Lock the ring by stepping three times on each tile except the last one.
        foreach ((int x, int y) in ring.Take(ring.Length - 1))
        {
            board.Mark(GameTeamColor.Red, Idx(x, y));
            board.Mark(GameTeamColor.Red, Idx(x, y));
            board.Mark(GameTeamColor.Red, Idx(x, y));
        }

        (int lastX, int lastY) = ring[^1];
        board.Mark(GameTeamColor.Red, Idx(lastX, lastY));
        board.Mark(GameTeamColor.Red, Idx(lastX, lastY));
        BanzaiMarkResult closing = board.Mark(GameTeamColor.Red, Idx(lastX, lastY));

        closing.Kind.Should().Be(BanzaiMarkKind.Lock);
        closing.RegionLocked.Should().ContainSingle().Which.Should().Be(Idx(2, 2));
        board.StateOf(Idx(2, 2)).Should().Be(5, "the pocket locks for red wholesale");
    }

    [Fact]
    public void ARegionTouchingTheOpenWorld_LeaksAndLocksNothing()
    {
        // Two tiles side by side: locking one leaves the other's fill escaping off the arena.
        BanzaiBoard board = Board((1, 1), (2, 1));
        int locking = Idx(1, 1);

        board.Mark(GameTeamColor.Red, locking);
        board.Mark(GameTeamColor.Red, locking);
        BanzaiMarkResult result = board.Mark(GameTeamColor.Red, locking);

        result.Kind.Should().Be(BanzaiMarkKind.Lock);
        result.RegionLocked.Should().BeEmpty("the neighbour region touches non-board positions");
        board.StateOf(Idx(2, 1)).Should().Be(1, "the leaking neighbour stays neutral");
    }

    [Fact]
    public void AForeignLockedTile_DoesNotBoundYourRegion()
    {
        // A would-be pocket whose wall is partly BLUE-locked leaks for red: only your own locked
        // tiles bound your fill.
        BanzaiBoard board = Board((1, 1), (2, 1));

        // Blue locks (2,1) legitimately.
        board.Mark(GameTeamColor.Blue, Idx(2, 1));
        board.Mark(GameTeamColor.Blue, Idx(2, 1));
        board.Mark(GameTeamColor.Blue, Idx(2, 1));

        // Red locks (1,1); its fill expands THROUGH blue's locked tile and leaks off-board.
        board.Mark(GameTeamColor.Red, Idx(1, 1));
        board.Mark(GameTeamColor.Red, Idx(1, 1));
        BanzaiMarkResult result = board.Mark(GameTeamColor.Red, Idx(1, 1));

        result.RegionLocked.Should().BeEmpty();
        board.StateOf(Idx(2, 1)).Should().Be(11, "blue's lock survives red's fill");
    }

    [Fact]
    public void OnlyTheLargestEnclosedPocket_Locks_TheArcturusQuirk()
    {
        // A walled corridor whose middle tile X separates a 1-tile pocket (left) from a 2-tile
        // pocket (right); the door D above X keeps every fill leaking to the open world while the
        // walls are built, so BOTH pockets close on the same lock event. Arcturus locks only the
        // larger one; this pins the mirrored quirk so a deliberate future change to all-pockets has
        // to flip a test, not discover a surprise.
        //
        //     W D W W          y=0 (D = door, never locked)
        //   C o X o o C        y=1 (C = caps, o = pockets, X = the locking tile)
        //     W W W W          y=2
        (int X, int Y)[] walls =
        [
            (1, 0),
            (3, 0),
            (4, 0),
            (1, 2),
            (2, 2),
            (3, 2),
            (4, 2),
            (0, 1),
            (5, 1),
        ];
        BanzaiBoard board = Board([.. walls, (2, 0), (1, 1), (2, 1), (3, 1), (4, 1)]);

        foreach ((int x, int y) in walls)
        {
            board.Mark(GameTeamColor.Red, Idx(x, y));
            board.Mark(GameTeamColor.Red, Idx(x, y));
            board.Mark(GameTeamColor.Red, Idx(x, y));
        }

        // Every wall lock so far leaked through the door; now X closes both pockets at once.
        board.Mark(GameTeamColor.Red, Idx(2, 1));
        board.Mark(GameTeamColor.Red, Idx(2, 1));
        BanzaiMarkResult result = board.Mark(GameTeamColor.Red, Idx(2, 1));

        result.Kind.Should().Be(BanzaiMarkKind.Lock);
        result.RegionLocked.Should().BeEquivalentTo([Idx(3, 1), Idx(4, 1)]);
        board.StateOf(Idx(1, 1)).Should().Be(1, "the smaller pocket stays neutral — the quirk");
        board.StateOf(Idx(2, 0)).Should().Be(1, "the door region leaks and never locks");
    }

    [Fact]
    public void AllLocked_IsTheEarlyEndCondition()
    {
        BanzaiBoard board = Board((1, 1), (2, 1));

        board.AllLocked().Should().BeFalse();

        foreach (int tile in new[] { Idx(1, 1), Idx(2, 1) })
        {
            board.Mark(GameTeamColor.Red, tile);
            board.Mark(GameTeamColor.Red, tile);
            board.Mark(GameTeamColor.Red, tile);
        }

        board.AllLocked().Should().BeTrue();
        board.LockedTilesOf(GameTeamColor.Red).Should().HaveCount(2);
    }

    [Fact]
    public void Deactivate_DarkensNeutralTiles_AndKeepsColours()
    {
        BanzaiBoard board = Board((1, 1), (2, 1));
        board.Mark(GameTeamColor.Red, Idx(1, 1));

        List<int> changed = board.Deactivate();

        changed.Should().ContainSingle().Which.Should().Be(Idx(2, 1));
        board.StateOf(Idx(2, 1)).Should().Be(0);
        board.StateOf(Idx(1, 1)).Should().Be(3, "claimed tiles keep their colour after the round");
        board.Mark(GameTeamColor.Red, Idx(1, 1)).Kind.Should().Be(BanzaiMarkKind.None);
    }

    [Fact]
    public void TheMapEdge_NeverWrapsARegionOntoTheNextRow()
    {
        // A tile at column 0: its "left neighbour" index would alias onto the previous row's last
        // column if the adjacency used bare idx-1. The fill must treat it as the open world.
        BanzaiBoard board = Board((0, 2));
        int tile = Idx(0, 2);

        board.Mark(GameTeamColor.Red, tile);
        board.Mark(GameTeamColor.Red, tile);
        BanzaiMarkResult result = board.Mark(GameTeamColor.Red, tile);

        result.Kind.Should().Be(BanzaiMarkKind.Lock);
        result.RegionLocked.Should().BeEmpty();
    }

    [Fact]
    public void ATeamlessWalker_MarksNothing()
    {
        BanzaiBoard board = Board((1, 1));

        board.Mark(GameTeamColor.None, Idx(1, 1)).Kind.Should().Be(BanzaiMarkKind.None);
        board.StateOf(Idx(1, 1)).Should().Be(1);
    }
}
