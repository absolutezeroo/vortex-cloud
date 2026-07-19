using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Room;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Players;

namespace Vortex.Rooms.Grains;

public sealed partial class RoomGrain
{
    public async Task<WiredRoomSettingsEventMessageComposer> GetWiredRoomSettingsAsync(
        PlayerId actor,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        RoomEntity? entity = await dbCtx
            .Rooms.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == _state.RoomId.Value, ct);

        return new WiredRoomSettingsEventMessageComposer
        {
            ModifyPermissionMask = entity?.WiredModifyPermissionMask ?? 0,
            ReadPermissionMask = entity?.WiredReadPermissionMask ?? 0,
            Timezone = entity?.WiredTimezone ?? string.Empty,
        };
    }

    public async Task<WiredRoomSettingsEventMessageComposer?> SetWiredRoomSettingsAsync(
        PlayerId actor,
        int modifyPermissionMask,
        int readPermissionMask,
        string timezone,
        CancellationToken ct
    )
    {
        if (_state.RoomSnapshot.OwnerId != actor)
        {
            return null;
        }

        await using TurboDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        RoomEntity? entity = await dbCtx.Rooms.FirstOrDefaultAsync(
            r => r.Id == _state.RoomId.Value,
            ct
        );

        if (entity is null)
        {
            return null;
        }

        entity.WiredModifyPermissionMask = modifyPermissionMask;
        entity.WiredReadPermissionMask = readPermissionMask;
        entity.WiredTimezone = timezone;

        await dbCtx.SaveChangesAsync(ct);

        return new WiredRoomSettingsEventMessageComposer
        {
            ModifyPermissionMask = modifyPermissionMask,
            ReadPermissionMask = readPermissionMask,
            Timezone = timezone,
        };
    }
}
