using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Database.Context;
using Turbo.Database.Entities.Marketplace;
using Turbo.Primitives.Furniture.Enums;
using Turbo.Primitives.Inventory.Grains;
using Turbo.Primitives.Inventory.Snapshots;
using Turbo.Primitives.Marketplace.Grains;
using Turbo.Primitives.Marketplace.Snapshots;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players.Enums.Wallet;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Players.Wallet;
using Turbo.Primitives.Rooms.Object;

namespace Turbo.Marketplace.Grains;

public sealed class MarketplacePurchaseGrain(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    IGrainFactory grainFactory,
    ILogger<MarketplacePurchaseGrain> logger
) : Grain, IMarketplacePurchaseGrain
{
    private const int COMMISSION_PERCENT = 1;
    private static readonly TimeSpan OFFER_DURATION = TimeSpan.FromDays(3);
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ILogger<MarketplacePurchaseGrain> _logger = logger;

    public async Task<(int Result, int OfferId)> MakeOfferAsync(
        int furnitureItemId,
        int price,
        CancellationToken ct
    )
    {
        if (price <= 0)
        {
            return (1, 0);
        }

        IInventoryGrain inventoryGrain = _grainFactory.GetInventoryGrain(this.GetPrimaryKeyLong());

        FurnitureItemSnapshot? snapshot = await inventoryGrain
            .GetItemSnapshotAsync(new RoomObjectId(furnitureItemId), ct)
            .ConfigureAwait(false);

        if (snapshot is null)
        {
            return (1, 0);
        }

        if (snapshot.RoomId.Value > 0)
        {
            return (1, 0);
        }

        if (!snapshot.Definition.CanSell)
        {
            return (1, 0);
        }

        bool removed = await inventoryGrain
            .RemoveFurnitureAsync(new RoomObjectId(furnitureItemId), ct)
            .ConfigureAwait(false);

        if (!removed)
        {
            return (1, 0);
        }

        int furniType = snapshot.Definition.ProductType == ProductType.Wall ? 2 : 1;

        MarketplaceOfferEntity offer = new()
        {
            SellerEntityId = (int)this.GetPrimaryKeyLong(),
            FurnitureDefinitionEntityId = snapshot.Definition.Id,
            SpriteId = snapshot.SpriteId,
            FurnitureType = furniType,
            ExtraData = snapshot.ExtraData,
            Price = price,
            State = MarketplaceOfferState.Active,
            CreditsOwed = 0,
            ExpiresAt = DateTime.UtcNow.Add(OFFER_DURATION),
        };

        await using TurboDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);
        dbCtx.MarketplaceOffers.Add(offer);
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        return (0, offer.Id);
    }

    public async Task<bool> CancelOrRedeemOfferAsync(int offerId, CancellationToken ct)
    {
        await using TurboDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        MarketplaceOfferEntity? offer = await dbCtx
            .MarketplaceOffers.FirstOrDefaultAsync(
                o => o.Id == offerId && o.SellerEntityId == (int)this.GetPrimaryKeyLong(),
                ct
            )
            .ConfigureAwait(false);

        if (offer is null)
        {
            return false;
        }

        if (offer.State == MarketplaceOfferState.Sold)
        {
            return false;
        }

        offer.State = MarketplaceOfferState.Cancelled;
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        IInventoryGrain inventoryGrain = _grainFactory.GetInventoryGrain(this.GetPrimaryKeyLong());
        await inventoryGrain
            .GrantFurnitureDefinitionAsync(offer.FurnitureDefinitionEntityId, offer.ExtraData, ct)
            .ConfigureAwait(false);

        return true;
    }

    public async Task<int> BuyOfferAsync(int offerId, CancellationToken ct)
    {
        await using TurboDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        DateTime now = DateTime.UtcNow;
        MarketplaceOfferEntity? offer = await dbCtx
            .MarketplaceOffers.FirstOrDefaultAsync(
                o =>
                    o.Id == offerId && o.State == MarketplaceOfferState.Active && o.ExpiresAt > now,
                ct
            )
            .ConfigureAwait(false);

        if (offer is null)
        {
            return 1;
        }

        IPlayerWalletGrain walletGrain = _grainFactory.GetPlayerWalletGrain(
            this.GetPrimaryKeyLong()
        );

        List<WalletDebitRequest> debitRequests =
        [
            new WalletDebitRequest
            {
                CurrencyKind = new CurrencyKind { CurrencyType = CurrencyType.Credits },
                Amount = offer.Price,
            },
        ];

        WalletPurchaseResult<bool> result = await walletGrain
            .ExecutePurchaseAsync(
                debitRequests,
                async innerCt =>
                {
                    int commission = Math.Max(1, offer.Price * COMMISSION_PERCENT / 100);
                    int creditsOwed = offer.Price - commission;

                    // Atomically claim the offer so a concurrent buyer cannot purchase it twice.
                    int claimed = await dbCtx
                        .MarketplaceOffers.Where(o =>
                            o.Id == offer.Id && o.State == MarketplaceOfferState.Active
                        )
                        .ExecuteUpdateAsync(
                            up =>
                                up.SetProperty(p => p.State, MarketplaceOfferState.Sold)
                                    .SetProperty(p => p.CreditsOwed, creditsOwed),
                            innerCt
                        )
                        .ConfigureAwait(false);

                    if (claimed == 0)
                    {
                        throw new InvalidOperationException(
                            $"Marketplace offer {offer.Id} is no longer active."
                        );
                    }

                    IInventoryGrain inventoryGrain = _grainFactory.GetInventoryGrain(
                        this.GetPrimaryKeyLong()
                    );

                    try
                    {
                        await inventoryGrain
                            .GrantFurnitureDefinitionAsync(
                                offer.FurnitureDefinitionEntityId,
                                offer.ExtraData,
                                innerCt
                            )
                            .ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // Compensate the claim so the offer is re-listed instead of staying
                        // Sold with no item delivered; the shared purchase helper then refunds
                        // the buyer when this exception bubbles up.
                        try
                        {
                            await dbCtx
                                .MarketplaceOffers.Where(o => o.Id == offer.Id)
                                .ExecuteUpdateAsync(
                                    up =>
                                        up.SetProperty(p => p.State, MarketplaceOfferState.Active)
                                            .SetProperty(p => p.CreditsOwed, 0),
                                    CancellationToken.None
                                )
                                .ConfigureAwait(false);
                        }
                        catch (Exception compensateEx)
                        {
                            _logger.LogError(
                                compensateEx,
                                "Failed to re-list marketplace offer {OfferId} after grant failure",
                                offer.Id
                            );
                        }

                        throw;
                    }

                    return true;
                },
                _logger,
                ct
            )
            .ConfigureAwait(false);

        return result.Succeeded ? 0 : 2;
    }

    public async Task<int> RedeemCreditsAsync(CancellationToken ct)
    {
        await using TurboDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        List<MarketplaceOfferEntity> soldOffers = await dbCtx
            .MarketplaceOffers.Where(o =>
                o.SellerEntityId == (int)this.GetPrimaryKeyLong()
                && o.State == MarketplaceOfferState.Sold
                && o.CreditsOwed > 0
            )
            .ToListAsync(ct)
            .ConfigureAwait(false);

        int totalCredits = soldOffers.Sum(o => o.CreditsOwed);

        if (totalCredits <= 0)
        {
            return 0;
        }

        foreach (MarketplaceOfferEntity o in soldOffers)
        {
            o.CreditsOwed = 0;
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        await _grainFactory
            .GetPlayerWalletGrain(this.GetPrimaryKeyLong())
            .GrantCreditsAsync(totalCredits, ct)
            .ConfigureAwait(false);

        return totalCredits;
    }

    public async Task<(int CreditsOwed, List<MarketplaceOfferSnapshot> Offers)> GetOwnOffersAsync(
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        DateTime now = DateTime.UtcNow;
        List<MarketplaceOfferEntity> offers = await dbCtx
            .MarketplaceOffers.Where(o =>
                o.SellerEntityId == (int)this.GetPrimaryKeyLong()
                && o.State != MarketplaceOfferState.Cancelled
            )
            .ToListAsync(ct)
            .ConfigureAwait(false);

        int creditsOwed = offers.Sum(o => o.CreditsOwed);

        List<MarketplaceOfferSnapshot> snapshots = offers
            .Select(o => new MarketplaceOfferSnapshot
            {
                OfferId = o.Id,
                SpriteId = o.SpriteId,
                FurnitureType = o.FurnitureType,
                ExtraData = o.ExtraData,
                Price = o.Price,
                AvgPrice = o.Price,
                OfferCount = 1,
                ExpiresIn =
                    o.State == MarketplaceOfferState.Active
                        ? (int)Math.Max(0, (o.ExpiresAt - now).TotalSeconds)
                        : 0,
                Status = (int)o.State,
                CreditsOwed = o.CreditsOwed,
            })
            .ToList();

        return (creditsOwed, snapshots);
    }
}
