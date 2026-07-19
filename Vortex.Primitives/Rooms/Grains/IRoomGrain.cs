using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Rooms.Events;

namespace Vortex.Primitives.Rooms.Grains;

public partial interface IRoomGrain : IGrainWithIntegerKey
{
    public Task DeactivateRoomAsync();
    public Task DelayRoomDeactivationAsync();
    public Task EnsureRoomActiveAsync(CancellationToken ct);
    public Task<RoomSnapshot> GetSnapshotAsync();
    public Task<RoomSummarySnapshot> GetSummaryAsync();
    public Task<int> GetRoomPopulationAsync();
    public Task<ImmutableArray<KeyValuePair<string, string>>> GetRoomPropertiesAsync();
    public Task PublishRoomEventAsync(RoomEvent evt, CancellationToken ct);
    public Task SendComposerToRoomAsync(IComposer composer);
}
