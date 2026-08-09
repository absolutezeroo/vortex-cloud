using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// Everything a player accumulates that is neither currency nor furniture: badges, avatar effects,
/// chat styles and saved outfits.
/// <para>
/// These tables are only ever read one player at a time by the game, so nothing in the emulator ever
/// looks at them hotel-wide — which is exactly where a broken grant shows up: a badge code held by
/// thousands, an effect nobody ever activated, a chat style owned by nobody because the catalogue
/// never sold it.
/// </para>
/// </summary>
internal sealed partial class DashboardApiService
{
    public Task<object> PlayerRewardsAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                int limit = ParseLimit(query["limit"], 25, 100);

                int totalBadges = await db
                    .PlayerBadges.AsNoTracking()
                    .CountAsync(b => b.DeletedAt == null, ct)
                    .ConfigureAwait(false);

                int equippedBadges = await db
                    .PlayerBadges.AsNoTracking()
                    .CountAsync(b => b.DeletedAt == null && b.SlotId != null, ct)
                    .ConfigureAwait(false);

                int playersWithBadges = await db
                    .PlayerBadges.AsNoTracking()
                    .Where(b => b.DeletedAt == null)
                    .Select(b => b.PlayerEntityId)
                    .Distinct()
                    .CountAsync(ct)
                    .ConfigureAwait(false);

                var topBadges = await db
                    .PlayerBadges.AsNoTracking()
                    .Where(b => b.DeletedAt == null)
                    .GroupBy(b => b.BadgeCode)
                    .Select(g => new
                    {
                        badgeCode = g.Key,
                        holders = g.Select(b => b.PlayerEntityId).Distinct().Count(),
                        equipped = g.Count(b => b.SlotId != null),
                    })
                    .OrderByDescending(g => g.holders)
                    .Take(limit)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                int totalEffects = await db
                    .PlayerEffects.AsNoTracking()
                    .CountAsync(e => e.DeletedAt == null, ct)
                    .ConfigureAwait(false);

                int activatedEffects = await db
                    .PlayerEffects.AsNoTracking()
                    .CountAsync(e => e.DeletedAt == null && e.ActivatedAt != null, ct)
                    .ConfigureAwait(false);

                int selectedEffects = await db
                    .PlayerEffects.AsNoTracking()
                    .CountAsync(e => e.DeletedAt == null && e.IsSelected, ct)
                    .ConfigureAwait(false);

                var topEffects = await db
                    .PlayerEffects.AsNoTracking()
                    .Where(e => e.DeletedAt == null)
                    .GroupBy(e => e.EffectId)
                    .Select(g => new
                    {
                        effectId = g.Key,
                        owners = g.Select(e => e.PlayerEntityId).Distinct().Count(),
                        activated = g.Count(e => e.ActivatedAt != null),
                        selected = g.Count(e => e.IsSelected),
                    })
                    .OrderByDescending(g => g.owners)
                    .Take(limit)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var chatStyles = await db
                    .PlayerChatStyles.AsNoTracking()
                    .OrderBy(s => s.ClientStyleId)
                    .Select(s => new
                    {
                        s.Id,
                        s.ClientStyleId,
                        owners = db.PlayerOwnedChatStyles.Count(o =>
                            o.ChatStyleId == s.Id && o.DeletedAt == null
                        ),
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                int wardrobeOutfits = await db
                    .PlayerWardrobeOutfits.AsNoTracking()
                    .CountAsync(o => o.DeletedAt == null, ct)
                    .ConfigureAwait(false);

                int wardrobeUsers = await db
                    .PlayerWardrobeOutfits.AsNoTracking()
                    .Where(o => o.DeletedAt == null)
                    .Select(o => o.PlayerEntityId)
                    .Distinct()
                    .CountAsync(ct)
                    .ConfigureAwait(false);

                var topCollectors = await db
                    .PlayerBadges.AsNoTracking()
                    .Where(b => b.DeletedAt == null)
                    .GroupBy(b => b.PlayerEntityId)
                    .Select(g => new { playerId = g.Key, badges = g.Count() })
                    .OrderByDescending(g => g.badges)
                    .Take(10)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                Dictionary<int, string> names = await LoadPlayerNamesAsync(
                        db,
                        NormalizeIds(topCollectors.Select(c => (int?)c.playerId)),
                        ct
                    )
                    .ConfigureAwait(false);

                return new
                {
                    totals = new
                    {
                        totalBadges,
                        equippedBadges,
                        playersWithBadges,
                        distinctBadgeCodes = await db
                            .PlayerBadges.AsNoTracking()
                            .Where(b => b.DeletedAt == null)
                            .Select(b => b.BadgeCode)
                            .Distinct()
                            .CountAsync(ct)
                            .ConfigureAwait(false),
                        totalEffects,
                        activatedEffects,
                        selectedEffects,
                        chatStyleCount = chatStyles.Count,
                        wardrobeOutfits,
                        wardrobeUsers,
                    },
                    topBadges,
                    topEffects,
                    chatStyles,
                    topCollectors = topCollectors
                        .Select(c => new
                        {
                            c.playerId,
                            playerName = ResolvePlayerName(names, c.playerId),
                            c.badges,
                        })
                        .ToList(),
                };
            },
            ct
        );

    /// <summary>Everything one player holds, for the investigation flow: their badges (equipped
    /// first), effects, chat styles and saved outfits in one call.</summary>
    public Task<object?> PlayerRewardDetailAsync(int playerId, CancellationToken ct) =>
        QueryAsync<object?>(
            async db =>
            {
                string? name = await db
                    .Players.AsNoTracking()
                    .Where(p => p.Id == playerId)
                    .Select(p => p.Name)
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);

                if (name is null)
                {
                    return null;
                }

                var badges = await db
                    .PlayerBadges.AsNoTracking()
                    .Where(b => b.PlayerEntityId == playerId && b.DeletedAt == null)
                    .OrderBy(b => b.SlotId == null)
                    .ThenBy(b => b.SlotId)
                    .Select(b => new
                    {
                        b.Id,
                        b.BadgeCode,
                        b.SlotId,
                        b.CreatedAt,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var effects = await db
                    .PlayerEffects.AsNoTracking()
                    .Where(e => e.PlayerEntityId == playerId && e.DeletedAt == null)
                    .OrderByDescending(e => e.IsSelected)
                    .ThenBy(e => e.EffectId)
                    .Select(e => new
                    {
                        e.Id,
                        e.EffectId,
                        e.SubType,
                        e.TotalDuration,
                        e.ActivatedAt,
                        e.IsSelected,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var chatStyles = await db
                    .PlayerOwnedChatStyles.AsNoTracking()
                    .Where(o => o.PlayerEntityId == playerId && o.DeletedAt == null)
                    .Select(o => new { o.Id, o.ChatStyleId })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var outfits = await db
                    .PlayerWardrobeOutfits.AsNoTracking()
                    .Where(o => o.PlayerEntityId == playerId && o.DeletedAt == null)
                    .OrderBy(o => o.SlotId)
                    .Select(o => new
                    {
                        o.Id,
                        o.SlotId,
                        o.Figure,
                        o.Gender,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                return new
                {
                    playerId,
                    playerName = name,
                    badges,
                    effects,
                    chatStyles,
                    outfits,
                };
            },
            ct
        );
}
