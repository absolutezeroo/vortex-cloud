using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Rooms.Configuration;

namespace Vortex.Rooms.Grains;

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
    private readonly Dictionary<RoomId, HashSet<PlayerId>> _roomPlayers = [];
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
        // The occupancy maps have to go with it. This grain is [KeepAlive], so anything left behind
        // here is kept for the lifetime of the silo — one stale entry per room ever opened.
        _activeRooms.Remove(roomId);
        _roomPlayers.Remove(roomId);
        _roomPopulations.Remove(roomId);

        return Task.CompletedTask;
    }

    public async Task AddPlayerToRoomAsync(PlayerId playerId, RoomId roomId, CancellationToken ct)
    {
        if (!_roomPlayers.TryGetValue(roomId, out HashSet<PlayerId>? playerIds))
        {
            playerIds = [];
            _roomPlayers[roomId] = playerIds;
        }

        playerIds.Add(playerId);

        await UpdatePopulationAsync(roomId);
    }

    public async Task RemovePlayerFromRoomAsync(
        PlayerId playerId,
        RoomId roomId,
        CancellationToken ct
    )
    {
        if (!_roomPlayers.TryGetValue(roomId, out HashSet<PlayerId>? players))
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
        _roomPopulations[roomId] = _roomPlayers.TryGetValue(roomId, out HashSet<PlayerId>? players)
            ? players.Count
            : 0;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Tells every active room whether to stay up. Fire-and-forget by construction: both calls are
    /// <c>[OneWay]</c>, so this turn hands off N messages and ends rather than waiting on N rooms.
    /// </summary>
    /// <remarks>
    /// This grain has one activation for the whole cluster and every room entry and exit in the
    /// hotel passes through it, so anything this method waits for, the whole hotel waits for.
    /// Awaiting the fan-out froze it for the length of the slowest room -- and a room that was
    /// itself mid-call to this directory could not answer until this turn ended, which is two
    /// non-reentrant grains waiting on each other until Orleans times one out.
    /// <para>
    /// ponytail: still one message per active room per sweep. That is a real cost at ten thousand
    /// rooms and it is no longer a stall -- sharding this grain by roomId bucket is the next step if
    /// the send itself starts to show.
    /// </para>
    /// </remarks>
    private Task CheckRoomsAsync(CancellationToken ct)
    {
        foreach (RoomActiveSnapshot room in _activeRooms.Values.ToArray())
        {
            int population = _roomPopulations.TryGetValue(room.RoomId, out int pop) ? pop : 0;
            IRoomCore roomGrain = _grainFactory.GetRoomCore(room.RoomId);

            // Discarded rather than logged: a one-way call's task is already completed when it
            // returns and can never carry a fault, so there is nothing to observe.
            if (population > 0)
            {
                _ = roomGrain.DelayRoomDeactivationAsync();
            }
            else
            {
                _ = roomGrain.DeactivateRoomAsync();
            }
        }

        return Task.CompletedTask;
    }
}
