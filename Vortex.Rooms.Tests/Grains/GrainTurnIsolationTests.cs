using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Grains;
using Xunit;

namespace Vortex.Rooms.Tests.Grains;

/// <summary>
/// AGENTS.md tells every contributor that grain code needs no locks because Orleans runs one turn at
/// a time per activation, and the grains here are written that way — plain <see cref="Dictionary{K,V}"/>
/// fields mutated with no synchronisation at all. That assumption is load-bearing and, until now,
/// asserted nowhere: a hand-constructed grain test calls one method at a time and so can never
/// contradict it. These tests hit a real activation concurrently.
/// </summary>
[Collection(VortexClusterCollection.Name)]
public sealed class GrainTurnIsolationTests(VortexClusterFixture cluster)
{
    private readonly VortexClusterFixture _cluster = cluster;

    [Fact]
    public async Task ConcurrentJoins_AreSerialisedIntoTheSameActivation()
    {
        IRoomDirectoryGrain directory = _cluster.GrainFactory.GetGrain<IRoomDirectoryGrain>(
            "turn-isolation-joins"
        );
        RoomId roomId = 900;
        const int Players = 250;

        // Unsynchronised dictionary writes under real parallelism lose entries or throw; the count
        // coming back exact is the guarantee itself, observed rather than assumed.
        await Task.WhenAll(
                Enumerable
                    .Range(1, Players)
                    .Select(id =>
                        directory.AddPlayerToRoomAsync((PlayerId)id, roomId, CancellationToken.None)
                    )
            )
            .ConfigureAwait(true);

        int population = await directory.GetRoomPopulationAsync(roomId).ConfigureAwait(true);

        population.Should().Be(Players);
    }

    [Fact]
    public async Task TheSamePlayerJoiningRepeatedly_IsCountedOnce()
    {
        IRoomDirectoryGrain directory = _cluster.GrainFactory.GetGrain<IRoomDirectoryGrain>(
            "turn-isolation-rejoin"
        );
        RoomId roomId = 901;
        PlayerId player = (PlayerId)1;

        // A reconnect races the old session's cleanup, so the same id really does arrive twice.
        await Task.WhenAll(
                Enumerable
                    .Range(0, 50)
                    .Select(_ =>
                        directory.AddPlayerToRoomAsync(player, roomId, CancellationToken.None)
                    )
            )
            .ConfigureAwait(true);

        int population = await directory.GetRoomPopulationAsync(roomId).ConfigureAwait(true);

        population.Should().Be(1);
    }

    [Fact]
    public async Task ClosingARoom_ForgetsItsOccupants()
    {
        // RoomDirectoryGrain is [KeepAlive], so anything it fails to drop here is held for as long
        // as the silo runs.
        IRoomDirectoryGrain directory = _cluster.GrainFactory.GetGrain<IRoomDirectoryGrain>(
            "turn-isolation-close"
        );
        RoomId roomId = 902;

        await directory
            .AddPlayerToRoomAsync((PlayerId)1, roomId, CancellationToken.None)
            .ConfigureAwait(true);
        await directory.RemoveActiveRoomAsync(roomId).ConfigureAwait(true);

        int population = await directory.GetRoomPopulationAsync(roomId).ConfigureAwait(true);

        population.Should().Be(0);
    }
}
