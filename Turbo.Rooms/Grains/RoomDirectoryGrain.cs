using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Players;
using Turbo.Primitives.Rooms;
using Turbo.Primitives.Rooms.Grains;
using Turbo.Rooms.Configuration;

namespace Turbo.Rooms.Grains;

[KeepAlive]
public class RoomDirectoryGrain(
    IOptions<RoomConfig> roomConfig,
    ILogger<IRoomDirectoryGrain> logger,
    IGrainFactory grainFactory
) : Grain, IRoomDirectoryGrain
{
    private readonly RoomConfig _roomConfig = roomConfig.Value;
    private readonly ILogger<IRoomDirectoryGrain> _logger = logger;
    private readonly IGrainFactory _grainFactory = grainFactory;

    private readonly Dictionary<RoomId, RoomActiveSnapshot> _activeRooms = [];
    private readonly Dictionary<RoomId, List<PlayerId>> _roomPlayers = [];
    private readonly Dictionary<RoomId, int> _roomPopulations = [];

    public override Task OnActivateAsync(CancellationToken ct)
    {
        this.RegisterGrainTimer<object?>(
            static async (self, tickCt) =>
                await ((RoomDirectoryGrain)self!).CheckRoomsAsync(tickCt),
            this,
            TimeSpan.FromMilliseconds(_roomConfig.RoomCheckMs),
            TimeSpan.FromMilliseconds(_roomConfig.RoomCheckMs)
        );

        return Task.CompletedTask;
    }

    public Task UpsertActiveRoomAsync(RoomInfoSnapshot snapshot)
    {
        if (snapshot is not null)
        {
            _activeRooms[snapshot.RoomId] = new RoomActiveSnapshot
            {
                RoomId = snapshot.RoomId,
                Name = snapshot.Name,
                Description = snapshot.Description,
                OwnerId = snapshot.OwnerId,
                OwnerName = snapshot.OwnerName,
                Population = 0,
                LastUpdatedUtc = DateTime.UtcNow,
            };
        }

        return Task.CompletedTask;
    }

    public Task RemoveActiveRoomAsync(RoomId roomId)
    {
        _activeRooms.Remove(roomId);

        return Task.CompletedTask;
    }

    public async Task AddPlayerToRoomAsync(PlayerId playerId, RoomId roomId, CancellationToken ct)
    {
        if (!_roomPlayers.TryGetValue(roomId, out List<PlayerId>? playerIds))
        {
            playerIds = [];
            _roomPlayers[roomId] = playerIds;
        }

        if (!playerIds.Contains(playerId))
        {
            playerIds.Add(playerId);
        }

        await UpdatePopulationAsync(roomId);
    }

    public async Task RemovePlayerFromRoomAsync(
        PlayerId playerId,
        RoomId roomId,
        CancellationToken ct
    )
    {
        if (!_roomPlayers.TryGetValue(roomId, out List<PlayerId>? players))
        {
            return;
        }

        if (!players.Remove(playerId))
        {
            return;
        }

        await UpdatePopulationAsync(roomId);
    }

    public Task<ImmutableArray<RoomSummarySnapshot>> GetActiveRoomsAsync() =>
        Task.FromResult(
            _activeRooms
                .Values.Select(x =>
                {
                    int population = _roomPopulations.TryGetValue(x.RoomId, out int pop) ? pop : 0;

                    return new RoomSummarySnapshot
                    {
                        RoomId = x.RoomId,
                        Name = x.Name,
                        Description = x.Description,
                        OwnerId = x.OwnerId,
                        OwnerName = x.OwnerName,
                        Population = population,
                        LastUpdatedUtc = x.LastUpdatedUtc,
                    };
                })
                .ToImmutableArray()
        );

    public Task<int> GetRoomPopulationAsync(RoomId roomId) =>
        Task.FromResult(_roomPopulations.TryGetValue(roomId, out int pop) ? pop : 0);

    private Task UpdatePopulationAsync(RoomId roomId)
    {
        _roomPopulations[roomId] = _roomPlayers.TryGetValue(roomId, out List<PlayerId>? players)
            ? players.Count
            : 0;

        return Task.CompletedTask;
    }

    private async Task CheckRoomsAsync(CancellationToken ct)
    {
        RoomActiveSnapshot[] rooms = _activeRooms.Values.ToArray();

        List<Task> pending = new(rooms.Length);

        foreach (RoomActiveSnapshot room in rooms)
        {
            int population = _roomPopulations.TryGetValue(room.RoomId, out int pop) ? pop : 0;
            IRoomGrain roomGrain = _grainFactory.GetRoomGrain(room.RoomId);

            pending.Add(
                population > 0
                    ? roomGrain.DelayRoomDeactivationAsync()
                    : roomGrain.DeactivateRoomAsync()
            );
        }

        await Task.WhenAll(pending).ConfigureAwait(true);
    }
}
