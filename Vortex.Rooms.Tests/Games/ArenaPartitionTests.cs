using System.Collections.Generic;
using FluentAssertions;
using Vortex.Primitives.Rooms.Object;
using Vortex.Rooms.Games.Arena;
using Xunit;

namespace Vortex.Rooms.Tests.Games;

/// <summary>
/// Cutting one game's furniture into independent installations, over hand-built positions and no
/// room. It is the generic answer to "a room may hold two Banzai boards": no per-game code, no
/// arena-id field on the wire, just how far apart the furniture is.
/// </summary>
public sealed class ArenaPartitionTests
{
    private static ArenaPlacement At(int id, int x, int y) => new(new RoomObjectId(id), x, y);

    [Fact]
    public void AGameThatDoesNotSeparate_IsAlwaysOneArena()
    {
        // Every Habbo game. The client cannot address a second board, so a second installation would
        // be unplayable rather than independent — and this path allocates and compares nothing.
        List<ArenaPlacement> spread = [At(1, 0, 0), At(2, 40, 40)];

        ArenaPartition partition = ArenaPartition.Build(spread, separation: 0);

        partition.InstanceCount.Should().Be(1);
        partition.InstanceOf(new RoomObjectId(1)).Should().Be(0);
        partition.InstanceOf(new RoomObjectId(2)).Should().Be(0);
    }

    [Fact]
    public void FurnitureWithinTheSeparation_IsOneInstallation()
    {
        List<ArenaPlacement> board = [At(1, 5, 5), At(2, 6, 5), At(3, 7, 5)];

        ArenaPartition partition = ArenaPartition.Build(board, separation: 2);

        partition.InstanceCount.Should().Be(1);
    }

    [Fact]
    public void TwoClustersFurtherApartThanTheSeparation_AreTwoInstallations()
    {
        List<ArenaPlacement> hall = [At(1, 1, 1), At(2, 2, 1), At(10, 20, 20), At(11, 21, 20)];

        ArenaPartition partition = ArenaPartition.Build(hall, separation: 3);

        partition.InstanceCount.Should().Be(2);
        partition.InstanceOf(new RoomObjectId(1)).Should().Be(0);
        partition.InstanceOf(new RoomObjectId(2)).Should().Be(0);
        partition.InstanceOf(new RoomObjectId(10)).Should().Be(1);
        partition.InstanceOf(new RoomObjectId(11)).Should().Be(1);
    }

    [Fact]
    public void ClustersJoinedByAChainOfFurniture_AreOneInstallation()
    {
        // Transitive: a row of tiles bridging two clusters makes them one board, which is what a
        // room owner who built one long arena means.
        List<ArenaPlacement> bridged = [At(1, 0, 0), At(2, 2, 0), At(3, 4, 0), At(4, 6, 0)];

        ArenaPartition.Build(bridged, separation: 2).InstanceCount.Should().Be(1);
    }

    [Fact]
    public void InstanceNumbering_IsStableAcrossRebuilds()
    {
        // An arena has to stay addressable from one tick to the next, so the numbering is a function
        // of the room's contents and not of dictionary ordering.
        List<ArenaPlacement> hall = [At(7, 1, 1), At(3, 20, 20), At(9, 2, 1)];

        ArenaPartition first = ArenaPartition.Build(hall, separation: 3);
        ArenaPartition second = ArenaPartition.Build(hall, separation: 3);

        foreach (ArenaPlacement placement in hall)
        {
            first.InstanceOf(placement.ObjectId).Should().Be(second.InstanceOf(placement.ObjectId));
        }
    }

    [Fact]
    public void AFootprint_MeasuresToItsNearestTile()
    {
        ArenaPartition partition = ArenaPartition.Build(
            [At(1, 10, 10), At(2, 11, 10)],
            separation: 2
        );

        ArenaFootprint footprint = partition.Footprints.Should().ContainSingle().Subject;

        footprint.DistanceTo(13, 10).Should().Be(2, "the nearest tile is (11,10)");
        footprint.DistanceTo(10, 10).Should().Be(0);
    }

    [Fact]
    public void AnEmptyRoom_StillHasOneArenaToRefuseToStart()
    {
        // A host with nothing in it must still exist: it is what reports the shortfall.
        ArenaPartition.Build([], separation: 4).InstanceCount.Should().Be(1);
    }
}
