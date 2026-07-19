using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Rooms.Grains;

public partial interface IRoomGrain
{
    public Task<WiredRoomSettingsEventMessageComposer> GetWiredRoomSettingsAsync(
        PlayerId actor,
        CancellationToken ct
    );

    public Task<WiredRoomSettingsEventMessageComposer?> SetWiredRoomSettingsAsync(
        PlayerId actor,
        int modifyPermissionMask,
        int readPermissionMask,
        string timezone,
        CancellationToken ct
    );
}
