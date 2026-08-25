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
using Vortex.Primitives.Events;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Inventory.Grains;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;

namespace Vortex.Collectibles.Grains;

/// <summary>
/// One player's Relics: the prizes waiting in the Collectors Guild, and handing them over.
/// </summary>
internal sealed class PlayerNftClaimsGrain(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    IGrainFactory grainFactory,
    IEventPublisher events,
    ILogger<PlayerNftClaimsGrain> logger
) : Grain, IPlayerNftClaimsGrain
{
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IEventPublisher _events = events;
    private readonly ILogger<PlayerNftClaimsGrain> _logger = logger;

    private PlayerId PlayerId => new((int)this.GetPrimaryKeyLong());

    public async Task<ImmutableArray<NftClaimSnapshot>> GetClaimsAsync(
        string wallet,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        NftClaimEntity[] claims = await OutstandingQuery(dbCtx)
            .OrderBy(claim => claim.Id)
            .ToArrayAsync(ct)
            .ConfigureAwait(true);

        if (claims.Length == 0)
        {
            return [];
        }

        Dictionary<string, FurnitureIdentity> definitions = await LoadDefinitionsAsync(
                dbCtx,
                [.. claims.Select(claim => claim.ProductCode)],
                ct
            )
            .ConfigureAwait(true);

        return [.. claims.Select(claim => ToSnapshot(claim, wallet, definitions))];
    }

    public async Task<int> ClaimAllAsync(CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        // Tracked, not AsNoTracking: the counters below are written back.
        List<NftClaimEntity> claims = await OutstandingQuery(dbCtx)
            .OrderBy(claim => claim.Id)
            .ToListAsync(ct)
            .ConfigureAwait(true);

        if (claims.Count == 0)
        {
            return 0;
        }

        Dictionary<string, FurnitureIdentity> definitions = await LoadDefinitionsAsync(
                dbCtx,
                [.. claims.Select(claim => claim.ProductCode)],
                ct
            )
            .ConfigureAwait(true);

        IInventoryGrain inventory = _grainFactory.GetInventoryGrain(PlayerId);
        int granted = 0;

        foreach (NftClaimEntity claim in claims)
        {
            if (!definitions.TryGetValue(claim.ProductCode, out FurnitureIdentity definition))
            {
                // A claim naming furniture that does not exist would consume the entitlement and
                // hand over nothing, so it is skipped and left outstanding for an admin to fix.
                _logger.LogError(
                    "Claim {ClaimCode} for player {PlayerId} names unknown furniture {ProductCode}; skipped.",
                    claim.ClaimCode,
                    PlayerId,
                    claim.ProductCode
                );

                continue;
            }

            int remaining = claim.ClaimLimit - claim.ClaimedAmount;

            for (int i = 0; i < remaining; i++)
            {
                await inventory
                    .GrantFurnitureDefinitionAsync(definition.DefinitionId, null, ct)
                    .ConfigureAwait(true);

                granted++;
            }

            // Counted after the grants rather than before: a failure above throws out of the whole
            // call, and an unclaimed prize is recoverable where a consumed one is not.
            claim.ClaimedAmount = claim.ClaimLimit;
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        _logger.LogInformation(
            "Player {PlayerId} claimed {Granted} collectible reward(s).",
            PlayerId,
            granted
        );

        if (granted > 0)
        {
            await _events
                .PublishAsync(new NftClaimsCollectedEvent(PlayerId, granted), ct)
                .ConfigureAwait(true);
        }

        return granted;
    }

    /// <summary>
    /// The claims a player may still take: not deleted, not already exhausted, and inside their
    /// validity window when they have one.
    /// </summary>
    private IQueryable<NftClaimEntity> OutstandingQuery(VortexDbContext dbCtx)
    {
        DateTime now = DateTime.UtcNow;

        return dbCtx.NftClaims.Where(claim =>
            claim.PlayerEntityId == PlayerId.Value
            && claim.DeletedAt == null
            && claim.ClaimedAmount < claim.ClaimLimit
            && (claim.ValidFrom == null || claim.ValidFrom <= now)
            && (claim.ValidTo == null || claim.ValidTo >= now)
        );
    }

    private static async Task<Dictionary<string, FurnitureIdentity>> LoadDefinitionsAsync(
        VortexDbContext dbCtx,
        string[] productCodes,
        CancellationToken ct
    )
    {
        return await FurnitureDefinitionLookup
            .ResolveByClassNameAsync(
                dbCtx,
                productCodes,
                definition => new FurnitureIdentity(
                    definition.Id,
                    definition.SpriteId,
                    definition.ProductType
                ),
                ct
            )
            .ConfigureAwait(true);
    }

    private static NftClaimSnapshot ToSnapshot(
        NftClaimEntity claim,
        string wallet,
        Dictionary<string, FurnitureIdentity> definitions
    )
    {
        definitions.TryGetValue(claim.ProductCode, out FurnitureIdentity definition);

        return new NftClaimSnapshot
        {
            ClaimId = claim.ClaimCode,
            Status = claim.Status,
            ClaimedAmount = claim.ClaimedAmount,
            ClaimLimit = claim.ClaimLimit,
            ValidFrom = ToUnixMs(claim.ValidFrom),
            ValidTo = ToUnixMs(claim.ValidTo),
            CreatedAt = ToUnixMs(claim.CreatedAt),
            UpdatedAt = ToUnixMs(claim.UpdatedAt),
            Collection = claim.Collection,
            ProductCode = claim.ProductCode,
            Wallet = wallet,
            ClaimItem = new NftClaimItemSnapshot
            {
                Product = new CollectibleProductItemSnapshot
                {
                    ProductTypeId = CollectibleProductIdentity.ForFurniture(definition.ProductType),
                    ItemTypeId = CollectibleProductIdentity.ItemTypeId(definition.SpriteId),
                    Score = 0,
                    ProductCode = claim.ProductCode,
                },
                SetId = claim.SetId,
                DefaultCollectionName = claim.DefaultCollectionName,
            },
        };
    }

    /// <summary>The client builds a <c>Date</c> straight from these, so they are milliseconds.</summary>
    private static long ToUnixMs(DateTime? value) =>
        value is null
            ? 0
            : new DateTimeOffset(
                DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            ).ToUnixTimeMilliseconds();

    private readonly record struct FurnitureIdentity(
        int DefinitionId,
        int SpriteId,
        ProductType ProductType
    );
}
