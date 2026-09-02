using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Logging;
using Vortex.Primitives.Rooms.Snapshots.Mapping;
using Vortex.Rooms.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Mapping;

/// <summary>
/// The other end of the tile-click family (ROOMM-TILE-041), swept for on the audit's own request:
/// every <c>ToIdx</c> followed by a direct index into the tile arrays. One was left — the tile
/// snapshot, which indexes five parallel arrays with the number it is handed and is a member of
/// <c>IRoomMap</c>, a public grain interface. Every caller in the repository passes an object's own
/// position, which is a fact about today's callers rather than a property of the method
/// (ROOMM-TILECALL-043).
/// </summary>
public sealed class RoomTileSnapshotBoundsTests
{
    [Fact]
    public async Task ATileInTheRoom_IsAnswered()
    {
        // The control: the guard must not have switched tile snapshots off.
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        RoomTileSnapshot tile = await harness
            .Grain.GetTileSnapshotAsync(1, 1, CancellationToken.None)
            .ConfigureAwait(true);

        tile.X.Should().Be(1);
        tile.Y.Should().Be(1);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    [InlineData(int.MaxValue, 1)]
    public async Task ATileThatIsNotOnTheMap_IsRefused(int x, int y)
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        Func<Task> read = () => harness.Grain.GetTileSnapshotAsync(x, y, CancellationToken.None);

        await read.Should().ThrowAsync<VortexException>().ConfigureAwait(true);
    }

    /// <summary>
    /// The fold, which is the case a sign check misses: x = Width is the first tile of the next row,
    /// so an unguarded read answers about a tile the caller never named.
    /// </summary>
    [Fact]
    public async Task ATileOneColumnPastTheEdge_IsRefusedRatherThanFolded()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        int width = harness.Grain.MapModule.Width;

        Func<Task> read = () =>
            harness.Grain.GetTileSnapshotAsync(width, 0, CancellationToken.None);

        await read.Should().ThrowAsync<VortexException>().ConfigureAwait(true);
    }
}
