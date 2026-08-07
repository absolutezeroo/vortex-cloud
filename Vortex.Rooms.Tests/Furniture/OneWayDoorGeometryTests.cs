using FluentAssertions;
using Vortex.Primitives.Rooms.Enums;
using Xunit;

namespace Vortex.Rooms.Tests.Furniture;

/// <summary>
/// A one-way gate is entirely geometry: the tile you must wait on is behind it and the tile you come
/// out on is in front, both derived from the gate's own rotation. Get the sign wrong and the gate
/// silently works backwards — passable only from the side it is meant to block, which is the one bug
/// a one-way gate must not have.
/// </summary>
public sealed class OneWayDoorGeometryTests
{
    [Theory]
    [InlineData(Rotation.North, 0, -1)]
    [InlineData(Rotation.NorthEast, 1, -1)]
    [InlineData(Rotation.East, 1, 0)]
    [InlineData(Rotation.SouthEast, 1, 1)]
    [InlineData(Rotation.South, 0, 1)]
    [InlineData(Rotation.SouthWest, -1, 1)]
    [InlineData(Rotation.West, -1, 0)]
    [InlineData(Rotation.NorthWest, -1, -1)]
    public void ToDelta_StepsOneTileTowardsTheFacing(Rotation rotation, int dx, int dy)
    {
        rotation.ToDelta().Should().Be((dx, dy));
    }

    [Theory]
    [InlineData(Rotation.North)]
    [InlineData(Rotation.NorthEast)]
    [InlineData(Rotation.East)]
    [InlineData(Rotation.SouthEast)]
    [InlineData(Rotation.South)]
    [InlineData(Rotation.SouthWest)]
    [InlineData(Rotation.West)]
    [InlineData(Rotation.NorthWest)]
    public void ToDelta_IsTheInverseOfFromDelta(Rotation rotation)
    {
        (int dx, int dy) = rotation.ToDelta();

        RotationExtensions.FromDelta(dx, dy).Should().Be(rotation);
    }

    [Theory]
    [InlineData(Rotation.North)]
    [InlineData(Rotation.East)]
    [InlineData(Rotation.South)]
    [InlineData(Rotation.West)]
    public void EntryAndExitTilesSitOnOppositeSidesOfTheGate(Rotation rotation)
    {
        const int gateX = 5;
        const int gateY = 5;

        (int dx, int dy) = rotation.ToDelta();

        (int entryX, int entryY) = (gateX - dx, gateY - dy);
        (int exitX, int exitY) = (gateX + dx, gateY + dy);

        // The gate is exactly between them, and they are two tiles apart — never the same tile,
        // which is what a zero delta would produce.
        ((entryX + exitX) / 2)
            .Should()
            .Be(gateX);
        ((entryY + exitY) / 2).Should().Be(gateY);
        (entryX == exitX && entryY == exitY).Should().BeFalse();

        // And you leave heading the way the gate points, not back the way you came.
        RotationExtensions.FromPoints(entryX, entryY, exitX, exitY).Should().Be(rotation);
    }
}
