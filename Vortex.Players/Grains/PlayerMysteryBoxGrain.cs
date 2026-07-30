using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.MysteryBox;
using Vortex.Primitives.Events;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Messages.Outgoing.Mysterybox;
using Vortex.Primitives.MysteryBox;
using Vortex.Primitives.MysteryBox.Grains;
using Vortex.Primitives.MysteryBox.Snapshots;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums;
using Vortex.Primitives.Players.Grains;

namespace Vortex.Players.Grains;

/// <summary>
/// Owns one player's side of the mystery box pairing. A DB-gateway grain (no persistent state, like
/// <see cref="PlayerEffectGrain"/>): every call opens a fresh context.
///
/// The two halves are stored differently on purpose. The box is real furniture, so ownership already
/// lives in the <c>furniture</c> table and is resolved here against the cached box definitions. The
/// key is not: furnidata has no key classname, and the client draws both tracker icons from its own
/// UI assets tinted by the colours this grain sends, so keys are server-side tokens in
/// <c>player_mystery_box_keys</c>.
/// </summary>
internal sealed class PlayerMysteryBoxGrain(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    IGrainFactory grainFactory,
    IFurnitureDefinitionProvider furnitureDefinitionProvider,
    IStuffDataFactory stuffDataFactory,
    IEventPublisher events,
    ILogger<PlayerMysteryBoxGrain> logger
) : Grain, IPlayerMysteryBoxGrain
{
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IFurnitureDefinitionProvider _furnitureDefinitionProvider =
        furnitureDefinitionProvider;
    private readonly IStuffDataFactory _stuffDataFactory = stuffDataFactory;
    private readonly IEventPublisher _events = events;
    private readonly ILogger<PlayerMysteryBoxGrain> _logger = logger;

    private int OwnerId => (int)this.GetPrimaryKeyLong();

    public async Task<MysteryBoxTrackerSnapshot> GetTrackerAsync(CancellationToken ct)
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            string boxColor = await ResolveBoxColorAsync(dbCtx, ct).ConfigureAwait(true);
            string keyColor = await ResolveKeyColorAsync(dbCtx, ct).ConfigureAwait(true);

            return new MysteryBoxTrackerSnapshot { BoxColor = boxColor, KeyColor = keyColor };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to resolve the mystery box tracker for player {PlayerId}",
                OwnerId
            );

            return MysteryBoxTrackerSnapshot.Empty;
        }
    }

    public async Task PushTrackerAsync(CancellationToken ct)
    {
        MysteryBoxTrackerSnapshot tracker = await GetTrackerAsync(ct).ConfigureAwait(true);

        await _grainFactory
            .GetPlayerPresenceGrain(OwnerId)
            .SendComposerAsync(
                new MysteryBoxKeysMessageComposer
                {
                    BoxColor = tracker.BoxColor,
                    KeyColor = tracker.KeyColor,
                }
            )
            .ConfigureAwait(true);
    }

    public async Task<bool> HasKeyAsync(string color, CancellationToken ct)
    {
        string normalized = MysteryBoxColors.Normalize(color);

        if (normalized.Length == 0)
        {
            return false;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            return await dbCtx
                .PlayerMysteryBoxKeys.AsNoTracking()
                .AnyAsync(
                    k =>
                        k.PlayerEntityId == OwnerId
                        && k.ConsumedAt == null
                        && k.DeletedAt == null
                        && k.Color == normalized,
                    ct
                )
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check mystery box keys for player {PlayerId}", OwnerId);

            return false;
        }
    }

    public async Task<bool> GrantKeyAsync(string color, string source, CancellationToken ct)
    {
        string normalized = MysteryBoxColors.Normalize(color);

        if (normalized.Length == 0)
        {
            _logger.LogWarning(
                "Refused to grant mystery box key of colour '{Color}' to player {PlayerId}: the client cannot render it.",
                color,
                OwnerId
            );

            return false;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            dbCtx.PlayerMysteryBoxKeys.Add(
                new PlayerMysteryBoxKeyEntity
                {
                    PlayerEntityId = OwnerId,
                    Color = normalized,
                    Source = source.Length > 64 ? source[..64] : source,
                }
            );

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to grant a {Color} mystery box key to player {PlayerId}",
                normalized,
                OwnerId
            );

            return false;
        }

        await _events
            .PublishAsync(new MysteryBoxKeyGrantedEvent(OwnerId, normalized, source), ct)
            .ConfigureAwait(true);
        await PushTrackerAsync(ct).ConfigureAwait(true);

        return true;
    }

    public async Task<bool> TryConsumeKeyAsync(string color, CancellationToken ct)
    {
        string normalized = MysteryBoxColors.Normalize(color);

        if (normalized.Length == 0)
        {
            return false;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            // Oldest first, and tracked rather than an ExecuteUpdate so the "did we actually spend
            // one?" answer comes from the same read that stamps it. Orleans serialises calls to this
            // grain, so no two opens can spend the same key.
            PlayerMysteryBoxKeyEntity? key = await dbCtx
                .PlayerMysteryBoxKeys.Where(k =>
                    k.PlayerEntityId == OwnerId
                    && k.ConsumedAt == null
                    && k.DeletedAt == null
                    && k.Color == normalized
                )
                .OrderBy(k => k.Id)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(true);

            if (key is null)
            {
                return false;
            }

            key.ConsumedAt = DateTime.UtcNow;

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to consume a {Color} mystery box key for player {PlayerId}",
                normalized,
                OwnerId
            );

            return false;
        }

        await _events
            .PublishAsync(new MysteryBoxKeyConsumedEvent(OwnerId, normalized), ct)
            .ConfigureAwait(true);
        await PushTrackerAsync(ct).ConfigureAwait(true);

        return true;
    }

    public async Task<MysteryBoxPrizeAward?> GrantPrizeAsync(
        MysteryBoxPrizeSnapshot prize,
        CancellationToken ct
    )
    {
        try
        {
            return prize.ProductType switch
            {
                ProductType.Floor or ProductType.Wall => await GrantFurniturePrizeAsync(prize, ct)
                    .ConfigureAwait(true),
                ProductType.Effect => await GrantEffectPrizeAsync(prize, ct).ConfigureAwait(true),
                ProductType.HabboClub => await GrantClubPrizeAsync(prize, ct).ConfigureAwait(true),
                _ => LogUndrawablePrize(prize),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to grant mystery box prize {PrizeId} to player {PlayerId}",
                prize.Id,
                OwnerId
            );

            return null;
        }
    }

    private MysteryBoxPrizeAward? LogUndrawablePrize(MysteryBoxPrizeSnapshot prize)
    {
        _logger.LogWarning(
            "Mystery box prize {PrizeId} has product type {ProductType}, which cannot be granted; player {PlayerId} received nothing.",
            prize.Id,
            prize.ProductType,
            OwnerId
        );

        return null;
    }

    private async Task<MysteryBoxPrizeAward?> GrantFurniturePrizeAsync(
        MysteryBoxPrizeSnapshot prize,
        CancellationToken ct
    )
    {
        FurnitureDefinitionSnapshot? definition = _furnitureDefinitionProvider.TryGetDefinition(
            prize.FurnitureDefinitionId
        );

        if (definition is null)
        {
            _logger.LogWarning(
                "Mystery box prize {PrizeId} points at furniture definition {DefinitionId}, which does not exist; player {PlayerId} received nothing.",
                prize.Id,
                prize.FurnitureDefinitionId,
                OwnerId
            );

            return null;
        }

        await _grainFactory
            .GetInventoryGrain(OwnerId)
            .GrantFurnitureDefinitionAsync(definition.Id, null, ct)
            .ConfigureAwait(true);

        // The reward window resolves furniture artwork by sprite id, not by our definition id.
        return new MysteryBoxPrizeAward
        {
            ContentType = definition.ProductType.ToLegacyString(),
            ClassId = definition.SpriteId,
        };
    }

    private async Task<MysteryBoxPrizeAward?> GrantEffectPrizeAsync(
        MysteryBoxPrizeSnapshot prize,
        CancellationToken ct
    )
    {
        // Same encoding the catalog uses: "effectId", "effectId:durationSeconds" or
        // "effectId:durationSeconds:subType" (duration 0/absent = permanent).
        string[] parts = prize.ExtraParam.Split(':');

        if (
            parts.Length == 0
            || !int.TryParse(
                parts[0],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int effectId
            )
            || effectId <= 0
        )
        {
            _logger.LogWarning(
                "Mystery box prize {PrizeId} has effect parameters '{ExtraParam}', which do not name an effect; player {PlayerId} received nothing.",
                prize.Id,
                prize.ExtraParam,
                OwnerId
            );

            return null;
        }

        int duration =
            parts.Length > 1
            && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int d)
                ? d
                : 0;
        int subType =
            parts.Length > 2
            && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int s)
                ? s
                : 0;

        await _grainFactory
            .GetPlayerEffectGrain(OwnerId)
            .AddEffectAsync(effectId, subType, duration, ct)
            .ConfigureAwait(true);

        return new MysteryBoxPrizeAward
        {
            ContentType = ProductType.Effect.ToLegacyString(),
            ClassId = effectId,
        };
    }

    private async Task<MysteryBoxPrizeAward?> GrantClubPrizeAsync(
        MysteryBoxPrizeSnapshot prize,
        CancellationToken ct
    )
    {
        // ExtraParam is the number of months, optionally suffixed with ":vip".
        string[] parts = prize.ExtraParam.Split(':');

        if (
            parts.Length == 0
            || !int.TryParse(
                parts[0],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int months
            )
            || months <= 0
        )
        {
            _logger.LogWarning(
                "Mystery box prize {PrizeId} has club parameters '{ExtraParam}', which do not name a month count; player {PlayerId} received nothing.",
                prize.Id,
                prize.ExtraParam,
                OwnerId
            );

            return null;
        }

        bool isVip = parts.Length > 1 && parts[1].Equals("vip", StringComparison.OrdinalIgnoreCase);

        ClubPurchaseResult result = await _grainFactory
            .GetPlayerGrain(OwnerId)
            .GrantClubMonthsAsync(months, isVip, ct)
            .ConfigureAwait(true);

        if (result != ClubPurchaseResult.Success)
        {
            _logger.LogWarning(
                "Mystery box prize {PrizeId} failed to extend club membership for player {PlayerId}: {Result}",
                prize.Id,
                OwnerId,
                result
            );

            return null;
        }

        // The club reward icon is looked up by product id; the prize row's definition id doubles as
        // that id so an operator can point it at whichever club product artwork they ship.
        return new MysteryBoxPrizeAward
        {
            ContentType = ProductType.HabboClub.ToLegacyString(),
            ClassId = prize.FurnitureDefinitionId,
        };
    }

    /// <summary>
    /// The colour of a mystery box the player owns (placed or in inventory). A box carries its colour
    /// in its own furniture state, so this reads the state back rather than consulting any table --
    /// which is what lets a single <c>mystery_box</c> definition cover all eight colours. Owning
    /// several shows the first coloured one; the tracker only has room for one.
    /// </summary>
    private async Task<string> ResolveBoxColorAsync(VortexDbContext dbCtx, CancellationToken ct)
    {
        ImmutableArray<int> boxDefinitionIds = await _grainFactory
            .GetMysteryBoxManagerGrain()
            .GetBoxDefinitionIdsAsync(ct)
            .ConfigureAwait(true);

        if (boxDefinitionIds.Length == 0)
        {
            return string.Empty;
        }

        int[] definitionIds = [.. boxDefinitionIds];

        List<string?> extraData = await dbCtx
            .Furnitures.AsNoTracking()
            .Where(f =>
                f.PlayerEntityId == OwnerId
                && f.DeletedAt == null
                && definitionIds.Contains(f.FurnitureDefinitionEntityId)
            )
            .OrderBy(f => f.Id)
            .Select(f => f.ExtraData)
            .ToListAsync(ct)
            .ConfigureAwait(true);

        foreach (string? blob in extraData)
        {
            // Read through the shared stuff-data factory rather than parsing the blob here: the
            // extra-data section shape is its convention, and a second reader would be free to drift
            // from it.
            int state = _stuffDataFactory
                .CreateStuffDataFromJson(StuffDataType.LegacyKey, blob)
                .GetState();
            string color = MysteryBoxSprite.ColorFromState(state);

            if (color.Length > 0)
            {
                return color;
            }
        }

        return string.Empty;
    }

    /// <summary>The colour of the oldest unspent key, so the tracker matches what the next open will
    /// actually spend.</summary>
    private async Task<string> ResolveKeyColorAsync(VortexDbContext dbCtx, CancellationToken ct)
    {
        string? color = await dbCtx
            .PlayerMysteryBoxKeys.AsNoTracking()
            .Where(k => k.PlayerEntityId == OwnerId && k.ConsumedAt == null && k.DeletedAt == null)
            .OrderBy(k => k.Id)
            .Select(k => k.Color)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(true);

        return MysteryBoxColors.Normalize(color);
    }
}
