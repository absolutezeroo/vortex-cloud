using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Turbo.Primitives.Players;

namespace Turbo.Primitives.Rooms.Grains;

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
