using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Primitives.Rooms.Grains;

public partial interface IRoomWired
{
    public Task<WiredRoomSettingsSnapshot> GetWiredRoomSettingsAsync(
        PlayerId actor,
        CancellationToken ct
    );

    public Task<WiredRoomSettingsSnapshot?> SetWiredRoomSettingsAsync(
        PlayerId actor,
        int modifyPermissionMask,
        int readPermissionMask,
        string timezone,
        CancellationToken ct
    );
}
