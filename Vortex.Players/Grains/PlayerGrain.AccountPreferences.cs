using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Orleans.Snapshots.Players;

namespace Vortex.Players.Grains;

internal sealed partial class PlayerGrain
{
    // Logical defaults for a player who has never touched the settings dialog. Volumes default to
    // full so a fresh account isn't silently muted; UI flags mirror the client's default expanded
    // panels (FriendBar | RoomTools = 1 | 2). Applied both when reading a missing row and when a
    // setter first creates the row, so persisting one setting never resets the others.
    private const int DefaultVolume = 100;
    private const int DefaultUiFlags = 3;

    public async Task<PlayerAccountPreferencesSnapshot> GetAccountPreferencesAsync(
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        PlayerAccountPreferencesEntity? entity = await dbCtx
            .PlayerAccountPreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlayerEntityId == _state.PlayerId.Value, ct);

        return new PlayerAccountPreferencesSnapshot
        {
            UiVolume = entity?.UiVolume ?? DefaultVolume,
            FurniVolume = entity?.FurniVolume ?? DefaultVolume,
            TraxVolume = entity?.TraxVolume ?? DefaultVolume,
            FreeFlowChatDisabled = entity?.FreeFlowChatDisabled ?? false,
            RoomInvitesIgnored = entity?.RoomInvitesIgnored ?? false,
            RoomCameraFollowDisabled = entity?.RoomCameraFollowDisabled ?? false,
            UiFlags = entity?.UiFlags ?? DefaultUiFlags,
        };
    }

    public Task SetSoundSettingsAsync(
        int uiVolume,
        int furniVolume,
        int traxVolume,
        CancellationToken ct
    ) =>
        UpdateAccountPreferencesAsync(
            e =>
            {
                e.UiVolume = ClampVolume(uiVolume);
                e.FurniVolume = ClampVolume(furniVolume);
                e.TraxVolume = ClampVolume(traxVolume);
            },
            ct
        );

    public Task SetFreeFlowChatDisabledAsync(bool disabled, CancellationToken ct) =>
        UpdateAccountPreferencesAsync(e => e.FreeFlowChatDisabled = disabled, ct);

    public Task SetRoomInvitesIgnoredAsync(bool ignored, CancellationToken ct) =>
        UpdateAccountPreferencesAsync(e => e.RoomInvitesIgnored = ignored, ct);

    public Task SetRoomCameraFollowDisabledAsync(bool disabled, CancellationToken ct) =>
        UpdateAccountPreferencesAsync(e => e.RoomCameraFollowDisabled = disabled, ct);

    public Task SetUiFlagsAsync(int flags, CancellationToken ct) =>
        UpdateAccountPreferencesAsync(e => e.UiFlags = flags, ct);

    private async Task UpdateAccountPreferencesAsync(
        Action<PlayerAccountPreferencesEntity> mutate,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        PlayerAccountPreferencesEntity? entity =
            await dbCtx.PlayerAccountPreferences.FirstOrDefaultAsync(
                p => p.PlayerEntityId == _state.PlayerId.Value,
                ct
            );

        if (entity is null)
        {
            entity = new PlayerAccountPreferencesEntity
            {
                PlayerEntityId = _state.PlayerId.Value,
                UiVolume = DefaultVolume,
                FurniVolume = DefaultVolume,
                TraxVolume = DefaultVolume,
                FreeFlowChatDisabled = false,
                RoomInvitesIgnored = false,
                RoomCameraFollowDisabled = false,
                UiFlags = DefaultUiFlags,
            };

            dbCtx.PlayerAccountPreferences.Add(entity);
        }

        mutate(entity);

        await dbCtx.SaveChangesAsync(ct);
    }

    private static int ClampVolume(int volume) => Math.Clamp(volume, 0, 100);
}
