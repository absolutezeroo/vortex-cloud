using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Rooms.Grains;

public sealed partial class RoomGrain
{
    public Task<WiredClickUserSnapshot> GetClickUserStateAsync(CancellationToken ct) =>
        WiredSystem.GetClickUserStateAsync(ct);
}
