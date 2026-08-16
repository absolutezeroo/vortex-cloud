using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Orleans.Snapshots.Players;

namespace Vortex.Players.Grains;

internal sealed partial class PlayerGrain
{
    public async Task<PlayerModeratorInfoSnapshot> GetModeratorInfoAsync(CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        int playerId = _state.PlayerId.Value;

        var account = await dbCtx
            .Players.AsNoTracking()
            .Where(p => p.Id == playerId)
            .Select(p => new
            {
                p.PlayerAccountEntityId,
                Email = p.PlayerAccount != null ? p.PlayerAccount.Email : null,
                p.LastLoginAt,
                p.TradingLockedUntil,
                p.CreatedAt,
            })
            .FirstOrDefaultAsync(ct);

        DateTime nowUtc = DateTime.UtcNow;

        // Bans are keyed by account, not by player: an alt sharing the login shares the ban count.
        int banCount = account?.PlayerAccountEntityId is int accountId
            ? await dbCtx.AccountBans.CountAsync(b => b.PlayerAccountEntityId == accountId, ct)
            : 0;

        int cfhCount = await dbCtx.CfhTickets.CountAsync(
            t => t.ReporterPlayerEntityId == playerId,
            ct
        );

        // The client calls this "abusive CFH count": reports this player filed that staff closed as
        // useless, which is the signal for someone spamming the report button.
        int abusiveCfhCount = await dbCtx.CfhTickets.CountAsync(
            t =>
                t.ReporterPlayerEntityId == playerId
                && t.CloseReason == CfhTicketCloseReason.Useless,
            ct
        );

        // "Cautions" on the client's card means "times this player was on the receiving end of a
        // ticket that ended in a sanction" — not the number of caution packets ever sent to them.
        int cautionCount = await dbCtx.CfhTickets.CountAsync(
            t => t.ReportedPlayerEntityId == playerId && t.Sanctioned,
            ct
        );

        DateTime? tradingLockedUntil = account?.TradingLockedUntil;
        bool tradingLockActive = tradingLockedUntil is not null && tradingLockedUntil > nowUtc;

        DateTime registeredAtUtc = account?.CreatedAt ?? _state.CreatedAt;

        return new PlayerModeratorInfoSnapshot
        {
            UserId = playerId,
            UserName = _state.Name,
            Figure = _state.Figure,
            RegistrationAgeInMinutes = ToWholeMinutes(nowUtc - registeredAtUtc),
            MinutesSinceLastLogin = account?.LastLoginAt is DateTime lastLogin
                ? ToWholeMinutes(nowUtc - lastLogin)
                : 0,
            CfhCount = cfhCount,
            AbusiveCfhCount = abusiveCfhCount,
            CautionCount = cautionCount,
            BanCount = banCount,
            // There is no trading-lock history table, only the current expiry, so the count is a
            // presence flag rather than a running total.
            TradingLockCount = tradingLockActive ? 1 : 0,
            TradingExpiryDate = tradingLockActive
                ? tradingLockedUntil!.Value.ToString("yyyy-MM-dd HH:mm")
                : string.Empty,
            PrimaryEmailAddress = account?.Email ?? string.Empty,
            IdentityId = account?.PlayerAccountEntityId ?? 0,
        };
    }

    /// <summary>Clamped at zero: a clock skew that puts the stored timestamp in the future must not
    /// surface as a negative age on the moderator's card.</summary>
    private static int ToWholeMinutes(TimeSpan span) =>
        span <= TimeSpan.Zero ? 0 : (int)Math.Min(span.TotalMinutes, int.MaxValue);

    /// <summary>
    /// Stamps this login and reports whether it is the day's first.
    /// </summary>
    /// <returns>
    /// True when the previous login was on an earlier day, or when there is no previous login at
    /// all. Returned rather than left to the caller because this is the only moment both values
    /// exist: the stamp below destroys the old one, and everything at login runs after it.
    /// </returns>
    public async Task<bool> MarkLoggedInAsync(CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        PlayerEntity? entity = await dbCtx.Players.FirstOrDefaultAsync(
            p => p.Id == _state.PlayerId.Value,
            ct
        );

        if (entity is null)
        {
            return false;
        }

        DateTime now = DateTime.UtcNow;

        // Compared in UTC, the same clock the column is written in. A hotel that wants the day to
        // turn over at its own local midnight would shift both, not just this read.
        bool isFirstLoginOfDay =
            entity.LastLoginAt is not DateTime previous || previous.Date < now.Date;

        entity.LastLoginAt = now;

        await dbCtx.SaveChangesAsync(ct);

        return isFirstLoginOfDay;
    }

    public async Task<PlayerModToolPreferencesSnapshot> GetModToolPreferencesAsync(
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        PlayerModToolPreferencesEntity? entity = await dbCtx
            .PlayerModToolPreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlayerEntityId == _state.PlayerId.Value, ct);

        return new PlayerModToolPreferencesSnapshot
        {
            WindowX = entity?.WindowX ?? 0,
            WindowY = entity?.WindowY ?? 0,
            WindowWidth = entity?.WindowWidth ?? 0,
            WindowHeight = entity?.WindowHeight ?? 0,
            IsSet = entity is not null,
        };
    }

    public async Task SetModToolPreferencesAsync(
        PlayerModToolPreferencesSnapshot preferences,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        PlayerModToolPreferencesEntity? entity =
            await dbCtx.PlayerModToolPreferences.FirstOrDefaultAsync(
                p => p.PlayerEntityId == _state.PlayerId.Value,
                ct
            );

        if (entity is null)
        {
            dbCtx.PlayerModToolPreferences.Add(
                new PlayerModToolPreferencesEntity
                {
                    PlayerEntityId = _state.PlayerId.Value,
                    WindowX = preferences.WindowX,
                    WindowY = preferences.WindowY,
                    WindowWidth = preferences.WindowWidth,
                    WindowHeight = preferences.WindowHeight,
                }
            );
        }
        else
        {
            entity.WindowX = preferences.WindowX;
            entity.WindowY = preferences.WindowY;
            entity.WindowWidth = preferences.WindowWidth;
            entity.WindowHeight = preferences.WindowHeight;
        }

        await dbCtx.SaveChangesAsync(ct);
    }
}
