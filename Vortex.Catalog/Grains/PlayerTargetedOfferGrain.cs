using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Catalog.TargetedOffers;
using Vortex.Database.Context;
using Vortex.Database.Entities.Catalog;
using Vortex.Primitives.Catalog.Grains;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Events;
using Vortex.Primitives.Inventory.Grains;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Wallet;

namespace Vortex.Catalog.Grains;

/// <summary>
/// Per-player targeted-offer grain: resolves the offer the player sees, performs the special-price
/// purchase (per-player limit + expiry enforced), and records tracking state. Orleans single-threads
/// per player, so the check-then-buy is race-free without locking.
/// </summary>
internal sealed class PlayerTargetedOfferGrain(
    IGrainFactory grainFactory,
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    IEventPublisher events,
    ILogger<PlayerTargetedOfferGrain> logger
) : Grain, IPlayerTargetedOfferGrain
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly IEventPublisher _events = events;
    private readonly ILogger<PlayerTargetedOfferGrain> _logger = logger;

    private int PlayerId => (int)this.GetPrimaryKeyLong();

    public async Task<TargetedOfferSnapshot?> GetCurrentOfferAsync(CancellationToken ct) =>
        await ResolveOfferAsync(afterOfferId: 0, ct).ConfigureAwait(true);

    public async Task<TargetedOfferSnapshot?> GetNextOfferAsync(
        int afterOfferId,
        CancellationToken ct
    ) => await ResolveOfferAsync(afterOfferId, ct).ConfigureAwait(true);

    public async Task<TargetedOfferSnapshot?> PurchaseAsync(
        int offerId,
        int quantity,
        CancellationToken ct
    )
    {
        TargetedOfferDefinitionSnapshot? definition = await GetDefinitionAsync(offerId, ct)
            .ConfigureAwait(true);

        if (definition is null)
        {
            return null;
        }

        Dictionary<int, PlayerTargetedOfferEntity> stateByOffer = await LoadStateAsync(ct)
            .ConfigureAwait(true);
        int purchaseCount = stateByOffer.GetValueOrDefault(offerId)?.PurchaseCount ?? 0;
        int trackingState = stateByOffer.GetValueOrDefault(offerId)?.TrackingState ?? 0;

        DateTime now = DateTime.Now;
        // Expired / over the per-player limit: nothing to buy, echo the current (unbuyable) state.
        if (!TargetedOfferMapper.CanPurchase(definition, purchaseCount, now))
        {
            return TargetedOfferMapper.ToWire(definition, purchaseCount, trackingState, now);
        }

        // Clamp the requested quantity to what the player may still buy.
        int remaining = TargetedOfferMapper.RemainingPurchases(definition, purchaseCount);
        int units = Math.Clamp(quantity, 1, Math.Min(remaining, 100));

        List<WalletDebitRequest> debits = BuildDebits(definition, units);

        WalletPurchaseResult<bool> result = await _grainFactory
            .GetPlayerWalletGrain(PlayerId)
            .ExecutePurchaseAsync(
                debits,
                async innerCt =>
                {
                    IInventoryGrain inventory = _grainFactory.GetInventoryGrain((long)PlayerId);

                    foreach (TargetedOfferProductSnapshot product in definition.Products)
                    {
                        if (product.FurnitureDefinitionId is not int definitionId)
                        {
                            continue;
                        }

                        for (int i = 0; i < product.Quantity * units; i++)
                        {
                            await inventory
                                .GrantFurnitureDefinitionAsync(definitionId, null, innerCt)
                                .ConfigureAwait(true);
                        }
                    }

                    return true;
                },
                _logger,
                ct
            )
            .ConfigureAwait(true);

        // Couldn't afford it: the wallet auto-refunded any partial debit; echo the unchanged offer.
        if (!result.Succeeded)
        {
            return TargetedOfferMapper.ToWire(
                definition,
                purchaseCount,
                trackingState,
                DateTime.Now
            );
        }

        int newCount = await IncrementPurchaseCountAsync(offerId, units, ct).ConfigureAwait(true);

        // Success path only: feeds the dashboard's targeted-offer purchase analytics via
        // TargetedOfferPurchasedAuditHandler (economy.targeted_offer_purchase). Non-transactional --
        // the furniture is already granted and the count already committed.
        await _events
            .PublishAsync(
                new TargetedOfferPurchasedEvent(
                    PlayerId,
                    offerId,
                    definition.Identifier,
                    units,
                    definition.PriceInCredits * units,
                    definition.PriceInActivityPoints * units
                ),
                ct
            )
            .ConfigureAwait(true);

        return TargetedOfferMapper.ToWire(definition, newCount, trackingState, DateTime.Now);
    }

    public async Task SetTrackingStateAsync(int offerId, int trackingState, CancellationToken ct)
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            PlayerTargetedOfferEntity? row = await dbCtx
                .PlayerTargetedOffers.FirstOrDefaultAsync(
                    p => p.PlayerEntityId == PlayerId && p.TargetedOfferEntityId == offerId,
                    ct
                )
                .ConfigureAwait(true);

            if (row is null)
            {
                dbCtx.PlayerTargetedOffers.Add(
                    new PlayerTargetedOfferEntity
                    {
                        PlayerEntityId = PlayerId,
                        TargetedOfferEntityId = offerId,
                        TrackingState = trackingState,
                    }
                );
            }
            else
            {
                row.TrackingState = trackingState;
            }

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to set targeted offer {OfferId} tracking for player {PlayerId}",
                offerId,
                PlayerId
            );
        }
    }

    private async Task<TargetedOfferSnapshot?> ResolveOfferAsync(
        int afterOfferId,
        CancellationToken ct
    )
    {
        ImmutableArray<TargetedOfferDefinitionSnapshot> definitions = await _grainFactory
            .GetTargetedOfferManagerGrain()
            .GetDefinitionsAsync(ct)
            .ConfigureAwait(true);

        if (definitions.IsEmpty)
        {
            return null;
        }

        Dictionary<int, PlayerTargetedOfferEntity> stateByOffer = await LoadStateAsync(ct)
            .ConfigureAwait(true);
        DateTime now = DateTime.Now;

        // The offers are ordered; "next" starts scanning after the given id.
        foreach (TargetedOfferDefinitionSnapshot definition in definitions)
        {
            if (afterOfferId > 0 && definition.Id <= afterOfferId)
            {
                continue;
            }

            int purchaseCount = stateByOffer.GetValueOrDefault(definition.Id)?.PurchaseCount ?? 0;
            if (!TargetedOfferMapper.CanPurchase(definition, purchaseCount, now))
            {
                continue;
            }

            int trackingState = stateByOffer.GetValueOrDefault(definition.Id)?.TrackingState ?? 0;
            return TargetedOfferMapper.ToWire(definition, purchaseCount, trackingState, now);
        }

        return null;
    }

    private static List<WalletDebitRequest> BuildDebits(
        TargetedOfferDefinitionSnapshot definition,
        int units
    )
    {
        List<WalletDebitRequest> debits = [];

        if (definition.PriceInCredits > 0)
        {
            debits.Add(
                new WalletDebitRequest
                {
                    CurrencyKind = new CurrencyKind { CurrencyType = CurrencyType.Credits },
                    Amount = definition.PriceInCredits * units,
                }
            );
        }

        if (definition.PriceInActivityPoints > 0)
        {
            debits.Add(
                new WalletDebitRequest
                {
                    CurrencyKind = new CurrencyKind
                    {
                        CurrencyType = CurrencyType.ActivityPoints,
                        ActivityPointType = definition.ActivityPointType,
                    },
                    Amount = definition.PriceInActivityPoints * units,
                }
            );
        }

        return debits;
    }

    private async Task<int> IncrementPurchaseCountAsync(
        int offerId,
        int units,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        PlayerTargetedOfferEntity? row = await dbCtx
            .PlayerTargetedOffers.FirstOrDefaultAsync(
                p => p.PlayerEntityId == PlayerId && p.TargetedOfferEntityId == offerId,
                ct
            )
            .ConfigureAwait(true);

        if (row is null)
        {
            row = new PlayerTargetedOfferEntity
            {
                PlayerEntityId = PlayerId,
                TargetedOfferEntityId = offerId,
                PurchaseCount = units,
            };
            dbCtx.PlayerTargetedOffers.Add(row);
        }
        else
        {
            row.PurchaseCount += units;
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
        return row.PurchaseCount;
    }

    private async Task<TargetedOfferDefinitionSnapshot?> GetDefinitionAsync(
        int offerId,
        CancellationToken ct
    )
    {
        ImmutableArray<TargetedOfferDefinitionSnapshot> definitions = await _grainFactory
            .GetTargetedOfferManagerGrain()
            .GetDefinitionsAsync(ct)
            .ConfigureAwait(true);

        return definitions.FirstOrDefault(d => d.Id == offerId);
    }

    private async Task<Dictionary<int, PlayerTargetedOfferEntity>> LoadStateAsync(
        CancellationToken ct
    )
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            List<PlayerTargetedOfferEntity> rows = await dbCtx
                .PlayerTargetedOffers.AsNoTracking()
                .Where(p => p.PlayerEntityId == PlayerId)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            return rows.ToDictionary(r => r.TargetedOfferEntityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to load targeted offer state for player {PlayerId}",
                PlayerId
            );
            return [];
        }
    }
}
