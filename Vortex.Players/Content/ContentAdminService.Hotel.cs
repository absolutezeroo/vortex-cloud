using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Catalog;
using Vortex.Database.Entities.Furniture;
using Vortex.Database.Entities.Players;
using Vortex.Database.Entities.Room;
using Vortex.Primitives.Content;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;

namespace Vortex.Players.Content;

/// <summary>
/// The hotel's smaller editable tables: hand items, bots, the currency/builders-club/rental rows,
/// and direct player grants.
/// <para>
/// Three different live-state rules apply here, and each one is enforced rather than assumed. Hand
/// items are read from the database every time a pet is fed, so a write is live immediately. A bot
/// standing in a room is owned by that room's grain, so editing its row would leave the two
/// disagreeing — those edits are refused until it is picked up. Player grants go through the
/// player's own grain where one exists, so the client is told rather than silently drifting.
/// </para>
/// </summary>
internal sealed partial class ContentAdminService
{
    public async Task<ContentAdminResult> UpsertHandItemAsync(
        HandItemSpec spec,
        CancellationToken ct
    )
    {
        if (spec.HandItemId <= 0)
        {
            return ContentAdminResult.Fail("hand_item_id_required");
        }

        if (string.IsNullOrWhiteSpace(spec.Name))
        {
            return ContentAdminResult.Fail("name_required");
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        HandItemEntity? entity = await db
            .HandItems.FirstOrDefaultAsync(h => h.HandItemId == spec.HandItemId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            entity = new HandItemEntity
            {
                HandItemId = spec.HandItemId,
                Name = spec.Name.Trim(),
                Nutrition = Math.Max(0, spec.Nutrition),
                Thirst = Math.Max(0, spec.Thirst),
            };

            db.HandItems.Add(entity);
        }
        else
        {
            entity.Name = spec.Name.Trim();
            entity.Nutrition = Math.Max(0, spec.Nutrition);
            entity.Thirst = Math.Max(0, spec.Thirst);
            entity.DeletedAt = null;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(entity.Id);
    }

    public async Task<ContentAdminResult> DeleteHandItemAsync(int handItemId, CancellationToken ct)
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        HandItemEntity? entity = await db
            .HandItems.FirstOrDefaultAsync(h => h.Id == handItemId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return ContentAdminResult.Fail("hand_item_not_found");
        }

        db.HandItems.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(handItemId);
    }

    public async Task<ContentAdminResult> UpdateBotAsync(
        int botId,
        BotSpec spec,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(spec.Name))
        {
            return ContentAdminResult.Fail("name_required");
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        BotEntity? entity = await db
            .Bots.FirstOrDefaultAsync(b => b.Id == botId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return ContentAdminResult.Fail("bot_not_found");
        }

        if (entity.RoomEntityId is not null)
        {
            // A placed bot is owned by its room's grain, which holds its own copy of the name,
            // figure and position. Writing the row underneath would leave the two disagreeing until
            // the room reloads, so the edit waits until the bot is back in its owner's hand.
            return ContentAdminResult.Fail("bot_is_placed");
        }

        entity.Name = spec.Name.Trim();
        entity.Motto = spec.Motto?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(spec.Figure))
        {
            entity.Figure = spec.Figure.Trim();
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(entity.Id);
    }

    public async Task<ContentAdminResult> DeleteBotAsync(int botId, CancellationToken ct)
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        BotEntity? entity = await db
            .Bots.FirstOrDefaultAsync(b => b.Id == botId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return ContentAdminResult.Fail("bot_not_found");
        }

        if (entity.RoomEntityId is not null)
        {
            return ContentAdminResult.Fail("bot_is_placed");
        }

        db.Bots.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(botId);
    }

    public async Task<ContentAdminResult> CreateCurrencyAsync(
        CurrencySpec spec,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(spec.Name))
        {
            return ContentAdminResult.Fail("name_required");
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        CurrencyTypeEntity entity = new()
        {
            Name = spec.Name.Trim(),
            CurrencyType = (Primitives.Players.Enums.Wallet.CurrencyType)spec.CurrencyType,
            ActivityPointType = spec.ActivityPointType,
            Enabled = spec.Enabled,
            StartingAmount = Math.Max(0, spec.StartingAmount),
        };

        db.CurrencyTypes.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadCurrenciesAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(entity.Id);
    }

    public async Task<ContentAdminResult> UpdateCurrencyAsync(
        int currencyId,
        CurrencySpec spec,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(spec.Name))
        {
            return ContentAdminResult.Fail("name_required");
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        CurrencyTypeEntity? entity = await db
            .CurrencyTypes.FirstOrDefaultAsync(c => c.Id == currencyId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return ContentAdminResult.Fail("currency_not_found");
        }

        entity.Name = spec.Name.Trim();
        entity.CurrencyType = (Primitives.Players.Enums.Wallet.CurrencyType)spec.CurrencyType;
        entity.ActivityPointType = spec.ActivityPointType;
        entity.Enabled = spec.Enabled;
        entity.StartingAmount = Math.Max(0, spec.StartingAmount);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadCurrenciesAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(entity.Id);
    }

    public async Task<ContentAdminResult> UpsertBuildersClubTierAsync(
        BuildersClubTierSpec spec,
        CancellationToken ct
    )
    {
        if (spec.Level <= 0 || spec.FurniLimit < 0)
        {
            return ContentAdminResult.Fail("invalid_tier");
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        BuildersClubTierEntity? entity = await db
            .BuildersClubTiers.FirstOrDefaultAsync(t => t.Level == spec.Level, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            entity = new BuildersClubTierEntity
            {
                Level = spec.Level,
                FurniLimit = spec.FurniLimit,
            };
            db.BuildersClubTiers.Add(entity);
        }
        else
        {
            entity.FurniLimit = spec.FurniLimit;
            entity.DeletedAt = null;
        }

        // Read straight from the table on every check, so nothing to reload.
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(entity.Id);
    }

    public async Task<ContentAdminResult> DeleteBuildersClubTierAsync(
        int tierId,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        BuildersClubTierEntity? entity = await db
            .BuildersClubTiers.FirstOrDefaultAsync(t => t.Id == tierId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return ContentAdminResult.Fail("tier_not_found");
        }

        db.BuildersClubTiers.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(tierId);
    }

    public async Task<ContentAdminResult> UpsertRentableSpaceTermsAsync(
        RentableSpaceTermsSpec spec,
        CancellationToken ct
    )
    {
        if (spec.FurnitureId <= 0 || spec.RentDurationSeconds <= 0)
        {
            return ContentAdminResult.Fail("invalid_terms");
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        // The entity requires both navigations, so they are loaded rather than set by id alone.
        FurnitureEntity? furniture = await db
            .Furnitures.FirstOrDefaultAsync(f => f.Id == spec.FurnitureId, ct)
            .ConfigureAwait(false);

        if (furniture is null)
        {
            return ContentAdminResult.Fail("furniture_not_found");
        }

        CurrencyTypeEntity? currency = await db
            .CurrencyTypes.FirstOrDefaultAsync(c => c.Id == spec.CurrencyTypeId, ct)
            .ConfigureAwait(false);

        if (currency is null)
        {
            return ContentAdminResult.Fail("currency_not_found");
        }

        RentableSpaceTermsEntity? entity = await db
            .RentableSpaceTerms.FirstOrDefaultAsync(
                t => t.FurnitureEntityId == spec.FurnitureId,
                ct
            )
            .ConfigureAwait(false);

        if (entity is null)
        {
            entity = new RentableSpaceTermsEntity
            {
                FurnitureEntityId = spec.FurnitureId,
                FurnitureEntity = furniture,
                Price = Math.Max(0, spec.Price),
                CurrencyTypeEntityId = spec.CurrencyTypeId,
                CurrencyTypeEntity = currency,
                RentDurationSeconds = spec.RentDurationSeconds,
                RequiresHc = spec.RequiresHc,
            };

            db.RentableSpaceTerms.Add(entity);
        }
        else
        {
            entity.Price = Math.Max(0, spec.Price);
            entity.CurrencyTypeEntityId = spec.CurrencyTypeId;
            entity.RentDurationSeconds = spec.RentDurationSeconds;
            entity.RequiresHc = spec.RequiresHc;
            entity.DeletedAt = null;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(entity.Id);
    }

    public async Task<ContentAdminResult> DeleteRentableSpaceTermsAsync(
        int termsId,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        RentableSpaceTermsEntity? entity = await db
            .RentableSpaceTerms.FirstOrDefaultAsync(t => t.Id == termsId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return ContentAdminResult.Fail("terms_not_found");
        }

        db.RentableSpaceTerms.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(termsId);
    }

    public async Task<ContentAdminResult> GrantBadgeAsync(
        int playerId,
        string badgeCode,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(badgeCode))
        {
            return ContentAdminResult.Fail("badge_code_required");
        }

        string code = badgeCode.Trim();

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        PlayerEntity? player = await db
            .Players.FirstOrDefaultAsync(p => p.Id == playerId, ct)
            .ConfigureAwait(false);

        if (player is null)
        {
            return ContentAdminResult.Fail("player_not_found");
        }

        PlayerBadgeEntity? existing = await db
            .PlayerBadges.FirstOrDefaultAsync(
                b => b.PlayerEntityId == playerId && b.BadgeCode == code,
                ct
            )
            .ConfigureAwait(false);

        if (existing is not null && existing.DeletedAt is null)
        {
            return ContentAdminResult.Fail("badge_already_held");
        }

        if (existing is not null)
        {
            existing.DeletedAt = null;
        }
        else
        {
            db.PlayerBadges.Add(
                new PlayerBadgeEntity
                {
                    PlayerEntityId = playerId,
                    PlayerEntity = player,
                    BadgeCode = code,
                }
            );
        }

        // The badge grain re-reads the table on every request rather than caching, so the row is the
        // whole grant — no reload to chase.
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(playerId);
    }

    public async Task<ContentAdminResult> RevokeBadgeAsync(
        int playerId,
        string badgeCode,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        PlayerBadgeEntity? entity = await db
            .PlayerBadges.FirstOrDefaultAsync(
                b => b.PlayerEntityId == playerId && b.BadgeCode == badgeCode,
                ct
            )
            .ConfigureAwait(false);

        if (entity is null)
        {
            return ContentAdminResult.Fail("badge_not_held");
        }

        db.PlayerBadges.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(playerId);
    }

    public async Task<ContentAdminResult> GrantEffectAsync(
        int playerId,
        int effectId,
        int durationSeconds,
        CancellationToken ct
    )
    {
        if (effectId <= 0)
        {
            return ContentAdminResult.Fail("effect_id_required");
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        if (!await db.Players.AnyAsync(p => p.Id == playerId, ct).ConfigureAwait(false))
        {
            return ContentAdminResult.Fail("player_not_found");
        }

        // Through the grain, not the table: it is what pushes AvatarEffectAdded, so a granted effect
        // appears in the client's list without a reconnect.
        await grainFactory
            .GetPlayerEffectGrain((PlayerId)playerId)
            .AddEffectAsync(effectId, 0, Math.Max(0, durationSeconds), ct)
            .ConfigureAwait(false);

        return ContentAdminResult.Ok(playerId);
    }

    public async Task<ContentAdminResult> RevokeEffectAsync(
        int playerId,
        int effectId,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        PlayerEffectEntity? entity = await db
            .PlayerEffects.FirstOrDefaultAsync(
                e => e.PlayerEntityId == playerId && e.EffectId == effectId && e.DeletedAt == null,
                ct
            )
            .ConfigureAwait(false);

        if (entity is null)
        {
            return ContentAdminResult.Fail("effect_not_owned");
        }

        db.PlayerEffects.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return ContentAdminResult.Ok(playerId);
    }

    private async Task ReloadCurrenciesAsync(CancellationToken ct)
    {
        try
        {
            await currencyTypes.ReloadAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Currency reference data reload failed after an admin write committed -- the live currency list is stale until the next reload or restart"
            );
            throw;
        }
    }
}
