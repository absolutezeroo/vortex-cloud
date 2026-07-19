using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Orleans.Snapshots.Players;

namespace Vortex.Players.Grains;

internal sealed partial class PlayerGrain
{
    public async Task<PlayerWiredPreferencesSnapshot> GetWiredPreferencesAsync(CancellationToken ct)
    {
        await using TurboDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        PlayerWiredPreferencesEntity? entity = await dbCtx
            .PlayerWiredPreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlayerEntityId == _state.PlayerId.Value, ct);

        return new PlayerWiredPreferencesSnapshot
        {
            WiredMenuButton = entity?.WiredMenuButton ?? false,
            WiredInspectButton = entity?.WiredInspectButton ?? false,
            PlayTestMode = entity?.PlayTestMode ?? false,
            WiredWhisperDisabled = entity?.WiredWhisperDisabled ?? false,
            ShowAllNotifications = entity?.ShowAllNotifications ?? false,
            UiStyle = entity?.UiStyle ?? string.Empty,
        };
    }

    public async Task SetWiredPreferencesAsync(
        PlayerWiredPreferencesSnapshot preferences,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        PlayerWiredPreferencesEntity? entity =
            await dbCtx.PlayerWiredPreferences.FirstOrDefaultAsync(
                p => p.PlayerEntityId == _state.PlayerId.Value,
                ct
            );

        if (entity is null)
        {
            dbCtx.PlayerWiredPreferences.Add(
                new PlayerWiredPreferencesEntity
                {
                    PlayerEntityId = _state.PlayerId.Value,
                    WiredMenuButton = preferences.WiredMenuButton,
                    WiredInspectButton = preferences.WiredInspectButton,
                    PlayTestMode = preferences.PlayTestMode,
                    WiredWhisperDisabled = preferences.WiredWhisperDisabled,
                    ShowAllNotifications = preferences.ShowAllNotifications,
                    UiStyle = preferences.UiStyle,
                }
            );
        }
        else
        {
            entity.WiredMenuButton = preferences.WiredMenuButton;
            entity.WiredInspectButton = preferences.WiredInspectButton;
            entity.PlayTestMode = preferences.PlayTestMode;
            entity.WiredWhisperDisabled = preferences.WiredWhisperDisabled;
            entity.ShowAllNotifications = preferences.ShowAllNotifications;
            entity.UiStyle = preferences.UiStyle;
        }

        await dbCtx.SaveChangesAsync(ct);
    }
}
