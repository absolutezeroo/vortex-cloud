using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Rooms.Events;

namespace Vortex.Primitives.Rooms.Grains;

/// <summary>Room activation lifecycle, the whole-room snapshots and the room-wide event/composer
/// fan-out — the facet every other room facet is layered on top of.</summary>
[Alias("Vortex.Primitives.Rooms.Grains.IRoomCore")]
public interface IRoomCore : IGrainWithIntegerKey
{
    public Task DeactivateRoomAsync();
    public Task DelayRoomDeactivationAsync();
    public Task EnsureRoomActiveAsync(CancellationToken ct);

    // Pure reads: they never mutate _state, so letting them interleave with the room tick (and each
    // other) keeps them off the queue behind a 50ms tick instead of serializing on it.
    [AlwaysInterleave]
    [ReadOnly]
    public Task<RoomSnapshot> GetSnapshotAsync();

    [AlwaysInterleave]
    [ReadOnly]
    public Task<RoomSummarySnapshot> GetSummaryAsync();
    public Task<int> GetRoomPopulationAsync();

    [AlwaysInterleave]
    [ReadOnly]
    public Task<ImmutableArray<KeyValuePair<string, string>>> GetRoomPropertiesAsync();
    public Task PublishRoomEventAsync(RoomEvent evt, CancellationToken ct);
    public Task SendComposerToRoomAsync(IComposer composer);
}
