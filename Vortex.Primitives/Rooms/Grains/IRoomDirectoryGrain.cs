using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Rooms.Grains;

public interface IRoomDirectoryGrain : IGrainWithStringKey
{
    public Task<ImmutableArray<RoomSummarySnapshot>> GetActiveRoomsAsync();
    public Task<int> GetRoomPopulationAsync(RoomId roomId);
    public Task UpsertActiveRoomAsync(RoomInfoSnapshot snapshot);
    public Task RemoveActiveRoomAsync(RoomId roomId);

    /// <summary>
    /// Records that a room has started, or finished, handling a raid — so the dashboard's live room
    /// list can show it without asking every room in the hotel.
    /// </summary>
    /// <remarks>
    /// Pushed by the room, and only when the state flips, which is at most twice per incident.
    /// Nothing else in the hotel would be able to answer this: an incident lives in the room's
    /// memory and never reaches the database.
    /// </remarks>
    public Task SetRaidIncidentAsync(RoomId roomId, bool active);
    public Task AddPlayerToRoomAsync(PlayerId playerId, RoomId roomId, CancellationToken ct);
    public Task RemovePlayerFromRoomAsync(PlayerId playerId, RoomId roomId, CancellationToken ct);
}
