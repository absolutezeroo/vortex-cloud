using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Vortex.Rooms.Grains.Systems.Freeze;
using Xunit;

namespace Vortex.Rooms.Tests.Freeze;

/// <summary>
/// Locks the shape of a snowball blast — the tile set the grain freezes/destroys off. A plain blast is a
/// plus of <c>radius + 1</c> arms; an X-blast (Mega power-up) adds the four diagonals. Geometry only, no
/// room state.
/// </summary>
public sealed class FreezeBlastGeometryTests
{
    private static HashSet<(int, int)> Tiles(int x, int y, int radius, bool diagonal) =>
        FreezeBlastGeometry.AffectedTiles(x, y, radius, diagonal).ToHashSet();

    [Fact]
    public void Plain_Blast_Is_A_Plus_Of_Radius_Plus_One_Arms()
    {
        // radius 0 => arm length 1: centre + one tile in each of the four cardinals.
        HashSet<(int, int)> tiles = Tiles(5, 5, radius: 0, diagonal: false);

        tiles
            .Should()
            .BeEquivalentTo(new HashSet<(int, int)> { (5, 5), (5, 4), (6, 5), (5, 6), (4, 5) });
    }

    [Fact]
    public void Boost_Lengthens_Every_Arm()
    {
        // radius 2 => arm length 3 in each cardinal direction. 1 centre + 4 * 3 = 13 tiles, no diagonals.
        HashSet<(int, int)> tiles = Tiles(0, 0, radius: 2, diagonal: false);

        tiles.Count.Should().Be(13);
        tiles
            .Should()
            .Contain((0, -3))
            .And.Contain((3, 0))
            .And.Contain((0, 3))
            .And.Contain((-3, 0));
        tiles.Should().NotContain((0, -4));
        tiles.Should().NotContain((1, 1)); // never a diagonal on a plain blast
    }

    [Fact]
    public void X_Blast_Adds_The_Four_Diagonals()
    {
        HashSet<(int, int)> plain = Tiles(0, 0, radius: 1, diagonal: false);
        HashSet<(int, int)> cross = Tiles(0, 0, radius: 1, diagonal: true);

        // Every plain tile is still covered.
        plain.Should().BeSubsetOf(cross);

        // Plus the diagonal arms (length radius + 1 = 2 each): 8 new tiles.
        (cross.Count - plain.Count)
            .Should()
            .Be(8);
        cross
            .Should()
            .Contain((1, -1))
            .And.Contain((2, -2))
            .And.Contain((1, 1))
            .And.Contain((2, 2))
            .And.Contain((-1, 1))
            .And.Contain((-2, 2))
            .And.Contain((-1, -1))
            .And.Contain((-2, -2));
    }
}
