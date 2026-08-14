using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Collectibles;
using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Collectibles.Grains;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Server.Grains;

namespace Vortex.Players.Grains;

/// <summary>
/// The minting side of the Collectors Guild: which furniture may be converted into a Relic, and
/// what stamps cost.
/// </summary>
/// <remarks>
/// <para>
/// Minting was long filed here as a blockchain errand, and it is not one. A Relic is an ordinary
/// piece of furniture the player already owns, converted; stamps are bought with silver. Both
/// currencies and both objects exist on this hotel, so the only thing that was ever missing is the
/// data an admin fills in.
/// </para>
/// <para>
/// Cached like the shop's offers — an admin edits them, players do not. The window on a mintable
/// type is re-checked against the clock on every read rather than baked into the cache, so an offer
/// closes on time without a reload.
/// </para>
/// </remarks>
[KeepAlive]
internal sealed class NftMintingGrain(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    IGrainFactory grainFactory,
    ILogger<NftMintingGrain> logger
) : Grain, INftMintingGrain
{
    /// <summary>
    /// The hotel-wide switch. Minting works, so it defaults to on; an admin turning it off makes the
    /// client hide the tab's whole minting half rather than leave dead buttons on screen.
    /// </summary>
    private const string MintingEnabledKey = "collectibles.minting.enabled";

    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ILogger<NftMintingGrain> _logger = logger;

    private ImmutableArray<CachedMintableType> _mintableTypes = [];
    private ImmutableArray<MintTokenOfferSnapshot> _tokenOffers = [];
    private bool _loaded;

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await LoadAsync(ct).ConfigureAwait(true);
        await base.OnActivateAsync(ct).ConfigureAwait(true);
    }

    public Task<bool> IsMintingEnabledAsync(CancellationToken ct) =>
        _grainFactory.GetServerConfigGrain().GetBoolAsync(MintingEnabledKey, true);

    public async Task<ImmutableArray<MintableItemTypeSnapshot>> GetMintableItemTypesAsync(
        CancellationToken ct
    )
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(true);

        DateTime now = DateTime.UtcNow;

        return [.. _mintableTypes.Where(type => IsOpen(type, now)).Select(ToSnapshot)];
    }

    public async Task<ImmutableArray<MintTokenOfferSnapshot>> GetTokenOffersAsync(
        CancellationToken ct
    )
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(true);

        return _tokenOffers;
    }

    public async Task<MintTokenOfferSnapshot?> FindTokenOfferAsync(
        int offerId,
        CancellationToken ct
    )
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(true);

        return _tokenOffers.FirstOrDefault(offer => offer.OfferId == offerId);
    }

    public async Task<MintableTypeTerms?> FindMintableTermsAsync(
        string productCode,
        CancellationToken ct
    )
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(true);

        DateTime now = DateTime.UtcNow;

        CachedMintableType? type = _mintableTypes.FirstOrDefault(candidate =>
            string.Equals(candidate.ProductCode, productCode, StringComparison.OrdinalIgnoreCase)
            && IsOpen(candidate, now)
        );

        return type is null
            ? null
            : new MintableTypeTerms
            {
                ProductCode = type.ProductCode,
                StampPrice = type.StampPrice,
            };
    }

    public Task ReloadAsync(CancellationToken ct) => LoadAsync(ct);

    /// <summary>
    /// Whether a type may be converted at this moment. The client makes the same judgement from the
    /// window it was sent — it disables the button once the end time passes — so a type that is
    /// closed here is one it would refuse anyway.
    /// </summary>
    private static bool IsOpen(CachedMintableType type, DateTime now) =>
        type.Enabled && type.StartsAt <= now && type.EndsAt > now;

    private async Task EnsureLoadedAsync(CancellationToken ct)
    {
        if (!_loaded)
        {
            await LoadAsync(ct).ConfigureAwait(true);
        }
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            NftMintableItemTypeEntity[] types = await dbCtx
                .NftMintableItemTypes.AsNoTracking()
                .Where(type => type.DeletedAt == null)
                .OrderBy(type => type.SortOrder)
                .ThenBy(type => type.Id)
                .ToArrayAsync(ct)
                .ConfigureAwait(true);

            string[] codes = [.. types.Select(type => type.ProductCode).Distinct()];

            // The client finds the player's copies of a mintable type by looking its *sprite* id up
            // in the inventory, so the id it is sent has to come from the definition. A classname
            // here would silently match a different piece of furniture, or none at all.
            Dictionary<string, (int SpriteId, ProductType Type)> definitions = await dbCtx
                .FurnitureDefinitions.AsNoTracking()
                .Where(definition =>
                    codes.Contains(definition.Name) && definition.DeletedAt == null
                )
                .ToDictionaryAsync(
                    definition => definition.Name,
                    definition => (definition.SpriteId, definition.ProductType),
                    StringComparer.OrdinalIgnoreCase,
                    ct
                )
                .ConfigureAwait(true);

            _mintableTypes =
            [
                .. types
                    .Select(type =>
                    {
                        bool known = definitions.TryGetValue(
                            type.ProductCode,
                            out (int SpriteId, ProductType Type) definition
                        );

                        if (!known)
                        {
                            // Listing it would draw sprite 0 and match nothing in the inventory, so
                            // the row is dropped and said so once, here, rather than puzzling
                            // somebody in front of the client.
                            _logger.LogError(
                                "Mintable type {ProductCode} names no furniture definition; leaving it out of the list.",
                                type.ProductCode
                            );
                        }

                        return known
                            ? new CachedMintableType(
                                type.ProductCode,
                                definition.SpriteId,
                                definition.Type,
                                type.StampPrice,
                                type.StartsAt,
                                type.EndsAt,
                                type.RegionLocked,
                                type.LimitedEdition,
                                type.Enabled
                            )
                            : null;
                    })
                    .OfType<CachedMintableType>(),
            ];

            _tokenOffers =
            [
                .. (
                    await dbCtx
                        .NftMintTokenOffers.AsNoTracking()
                        .Where(offer => offer.DeletedAt == null && offer.Enabled)
                        .OrderBy(offer => offer.SortOrder)
                        .ThenBy(offer => offer.Id)
                        .ToArrayAsync(ct)
                        .ConfigureAwait(true)
                ).Select(offer => new MintTokenOfferSnapshot
                {
                    OfferId = offer.Id,
                    ProductCode = offer.ProductCode,
                    SilverPrice = offer.SilverPrice,
                    AmountTokens = offer.AmountTokens,
                }),
            ];

            _loaded = true;

            _logger.LogInformation(
                "Loaded {TypeCount} mintable item type(s) and {OfferCount} stamp bundle(s).",
                _mintableTypes.Length,
                _tokenOffers.Length
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the collectibles minting configuration.");
        }
    }

    private static MintableItemTypeSnapshot ToSnapshot(CachedMintableType type) =>
        new()
        {
            ItemTypeId = type.SpriteId,
            StartTime = ToUnixSeconds(type.StartsAt),
            EndTime = ToUnixSeconds(type.EndsAt),
            RegionLocked = type.RegionLocked,
            Price = type.StampPrice,
            LimitedEdition = type.LimitedEdition,
            ItemType = MintableItemKind.ForFurniture(type.ProductType),
        };

    /// <summary>
    /// Seconds, not milliseconds: the client multiplies both times by 1000 itself before comparing
    /// them to a date. Sending milliseconds would put every window tens of thousands of years out.
    /// </summary>
    private static int ToUnixSeconds(DateTime value) =>
        (int)new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)).ToUnixTimeSeconds();

    private sealed record CachedMintableType(
        string ProductCode,
        int SpriteId,
        ProductType ProductType,
        int StampPrice,
        DateTime StartsAt,
        DateTime EndsAt,
        bool RegionLocked,
        bool LimitedEdition,
        bool Enabled
    );
}
