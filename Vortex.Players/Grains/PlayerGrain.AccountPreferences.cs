using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Events;
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

    // 0, deliberately: the client reads it as "never answered", falls back to its own all-on
    // defaults for display, and shows the Discord opt-in popup once. Seeding the current version
    // here would suppress that popup for every player who never saw it.
    private const int DefaultDiscordVersion = 0;

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
            DiscordSettingsVersion = entity?.DiscordSettingsVersion ?? DefaultDiscordVersion,
            DiscordShowHabbo = entity?.DiscordShowHabbo ?? true,
            DiscordShareActivity = entity?.DiscordShareActivity ?? true,
            DiscordHideInHiddenRooms = entity?.DiscordHideInHiddenRooms ?? true,
            DiscordAllowJoining = entity?.DiscordAllowJoining ?? true,
        };
    }

    public Task SetSoundSettingsAsync(
        int uiVolume,
        int furniVolume,
        int traxVolume,
        CancellationToken ct
    ) =>
        UpdateAccountPreferencesAsync(
            "sound",
            e =>
            {
                e.UiVolume = ClampVolume(uiVolume);
                e.FurniVolume = ClampVolume(furniVolume);
                e.TraxVolume = ClampVolume(traxVolume);
            },
            ct
        );

    public Task SetFreeFlowChatDisabledAsync(bool disabled, CancellationToken ct) =>
        UpdateAccountPreferencesAsync("chat", e => e.FreeFlowChatDisabled = disabled, ct);

    public Task SetRoomInvitesIgnoredAsync(bool ignored, CancellationToken ct) =>
        UpdateAccountPreferencesAsync("room_invites", e => e.RoomInvitesIgnored = ignored, ct);

    public Task SetRoomCameraFollowDisabledAsync(bool disabled, CancellationToken ct) =>
        UpdateAccountPreferencesAsync("camera", e => e.RoomCameraFollowDisabled = disabled, ct);

    public Task SetUiFlagsAsync(int flags, CancellationToken ct) =>
        UpdateAccountPreferencesAsync("ui_flags", e => e.UiFlags = flags, ct);

    public Task SetDiscordPreferencesAsync(
        int version,
        bool showHabbo,
        bool shareActivity,
        bool hideInHiddenRooms,
        bool allowJoining,
        CancellationToken ct
    ) =>
        UpdateAccountPreferencesAsync(
            "discord",
            e =>
            {
                e.DiscordSettingsVersion = version;
                e.DiscordShowHabbo = showHabbo;
                e.DiscordShareActivity = shareActivity;
                e.DiscordHideInHiddenRooms = hideInHiddenRooms;
                e.DiscordAllowJoining = allowJoining;
            },
            ct
        );

    /// <summary>
    /// The one write path for every preference, so the record is raised here rather than in five
    /// setters that would each have to remember. <paramref name="setting"/> names which pane the
    /// client changed: "he turned free-flow chat off just before the incident" is a real question,
    /// and a single "preferences changed" line cannot answer it.
    /// </summary>
    private async Task UpdateAccountPreferencesAsync(
        string setting,
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
                DiscordSettingsVersion = DefaultDiscordVersion,
                DiscordShowHabbo = true,
                DiscordShareActivity = true,
                DiscordHideInHiddenRooms = true,
                DiscordAllowJoining = true,
            };

            dbCtx.PlayerAccountPreferences.Add(entity);
        }

        mutate(entity);

        await dbCtx.SaveChangesAsync(ct);

        await _events
            .PublishAsync(new AccountPreferenceChangedEvent(_state.PlayerId, setting), ct)
            .ConfigureAwait(true);
    }

    private static int ClampVolume(int volume) => Math.Clamp(volume, 0, 100);
}
