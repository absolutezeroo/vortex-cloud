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

public partial interface IRoomGrain : IGrainWithIntegerKey
{
    public Task DeactivateRoomAsync();
    public Task DelayRoomDeactivationAsync();
    public Task EnsureRoomActiveAsync(CancellationToken ct);

    // Pure reads: they never mutate _state, so letting them interleave with the room tick (and each
    // other) keeps them off the queue behind a 50ms tick instead of serializing on it.
    [AlwaysInterleave]
    public Task<RoomSnapshot> GetSnapshotAsync();

    [AlwaysInterleave]
    public Task<RoomSummarySnapshot> GetSummaryAsync();
    public Task<int> GetRoomPopulationAsync();

    [AlwaysInterleave]
    public Task<ImmutableArray<KeyValuePair<string, string>>> GetRoomPropertiesAsync();
    public Task PublishRoomEventAsync(RoomEvent evt, CancellationToken ct);
    public Task SendComposerToRoomAsync(IComposer composer);
}
