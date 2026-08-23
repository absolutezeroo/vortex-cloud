using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Room;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Rooms.Grains;

public sealed partial class RoomGrain
{
    public async Task<WiredRoomSettingsSnapshot> GetWiredRoomSettingsAsync(
        PlayerId actor,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        RoomEntity? entity = await dbCtx
            .Rooms.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == _state.RoomId.Value, ct);

        return new WiredRoomSettingsSnapshot
        {
            ModifyPermissionMask = entity?.WiredModifyPermissionMask ?? 0,
            ReadPermissionMask = entity?.WiredReadPermissionMask ?? 0,
            Timezone = entity?.WiredTimezone ?? string.Empty,
        };
    }

    public async Task<WiredRoomSettingsSnapshot?> SetWiredRoomSettingsAsync(
        PlayerId actor,
        int modifyPermissionMask,
        int readPermissionMask,
        string timezone,
        CancellationToken ct
    )
    {
        if (!await IsRoomOwnerAsync(actor).ConfigureAwait(true))
        {
            return null;
        }

        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

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

        return new WiredRoomSettingsSnapshot
        {
            ModifyPermissionMask = modifyPermissionMask,
            ReadPermissionMask = readPermissionMask,
            Timezone = timezone,
        };
    }
}
