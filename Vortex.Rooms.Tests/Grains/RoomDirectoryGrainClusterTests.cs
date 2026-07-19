using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.TestingHost;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Rooms.Configuration;
using Xunit;

namespace Vortex.Rooms.Tests.Grains;

/// <summary>
/// Proof of concept for testing a real Orleans grain end-to-end inside an in-process silo
/// (as opposed to the hand-constructed grain tests in Vortex.Rooms.Tests/Groups), so activation,
/// DI wiring, and grain-reference calls are exercised the same way production hosting exercises
/// them. <see cref="RoomDirectoryGrain"/> was picked because it has no database dependency.
/// </summary>
public sealed class RoomDirectoryGrainClusterTests : IAsyncLifetime
{
    private TestCluster _cluster = null!;

    public async Task InitializeAsync()
    {
        TestClusterBuilder builder = new TestClusterBuilder(1);
        builder.AddSiloBuilderConfigurator<SiloConfigurator>();
        _cluster = builder.Build();

        await _cluster.DeployAsync().ConfigureAwait(true);
    }

    public async Task DisposeAsync()
    {
        await _cluster.StopAllSilosAsync().ConfigureAwait(true);

        _cluster.Dispose();
    }

    [Fact]
    public async Task UpsertActiveRoomAsync_ThenGetActiveRoomsAsync_ReturnsUpsertedRoom()
    {
        IRoomDirectoryGrain grain = _cluster.GrainFactory.GetGrain<IRoomDirectoryGrain>(
            "cluster-poc-directory"
        );
        RoomId roomId = 42;
        RoomInfoSnapshot snapshot = new RoomInfoSnapshot
        {
            RoomId = roomId,
            Name = "Cluster Test Room",
            Description = "poc",
            OwnerId = (PlayerId)1,
            OwnerName = "Owner",
            Population = 0,
            LastUpdatedUtc = DateTime.UtcNow,
            DoorMode = RoomDoorModeType.Open,
            PlayersMax = 25,
            TradeType = RoomTradeModeType.Disabled,
            Score = 0,
            Ranking = 0,
            CategoryId = -1,
            Tags = [],
            StaffPick = false,
            AllowBlocking = false,
            AllowPets = false,
            AllowPetsEat = false,
            PaintWall = 0.0,
            PaintFloor = 0.0,
            PaintLandscape = 0.0,
        };

        await grain.UpsertActiveRoomAsync(snapshot).ConfigureAwait(true);
        ImmutableArray<RoomSummarySnapshot> rooms = await grain
            .GetActiveRoomsAsync()
            .ConfigureAwait(true);

        rooms.Should().ContainSingle(r => r.RoomId == roomId && r.Name == "Cluster Test Room");
    }

    [Fact]
    public async Task RemoveActiveRoomAsync_RemovesPreviouslyUpsertedRoom()
    {
        IRoomDirectoryGrain grain = _cluster.GrainFactory.GetGrain<IRoomDirectoryGrain>(
            "cluster-poc-directory-remove"
        );
        RoomId roomId = 43;
        RoomInfoSnapshot snapshot = new RoomInfoSnapshot
        {
            RoomId = roomId,
            Name = "Removable Room",
            Description = "poc",
            OwnerId = (PlayerId)1,
            OwnerName = "Owner",
            Population = 0,
            LastUpdatedUtc = DateTime.UtcNow,
            DoorMode = RoomDoorModeType.Open,
            PlayersMax = 25,
            TradeType = RoomTradeModeType.Disabled,
            Score = 0,
            Ranking = 0,
            CategoryId = -1,
            Tags = [],
            StaffPick = false,
            AllowBlocking = false,
            AllowPets = false,
            AllowPetsEat = false,
            PaintWall = 0.0,
            PaintFloor = 0.0,
            PaintLandscape = 0.0,
        };

        await grain.UpsertActiveRoomAsync(snapshot).ConfigureAwait(true);
        await grain.RemoveActiveRoomAsync(roomId).ConfigureAwait(true);
        ImmutableArray<RoomSummarySnapshot> rooms = await grain
            .GetActiveRoomsAsync()
            .ConfigureAwait(true);

        rooms.Should().BeEmpty();
    }

    private sealed class SiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddOptions<RoomConfig>();
            });
        }
    }
}
