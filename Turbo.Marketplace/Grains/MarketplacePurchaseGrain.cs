using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Orleans;
using Turbo.Database.Context;
using Turbo.Database.Entities.Marketplace;
using Turbo.Primitives.Marketplace.Grains;
using Turbo.Primitives.Marketplace.Snapshots;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players.Enums.Wallet;
using Turbo.Primitives.Players.Wallet;
using Turbo.Primitives.Rooms.Object;

namespace Turbo.Marketplace.Grains;

public sealed class MarketplacePurchaseGrain(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    IGrainFactory grainFactory
) : Grain, IMarketplacePurchaseGrain
{
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly IGrainFactory _grainFactory = grainFactory;

    private const int COMMISSION_PERCENT = 1;
    private static readonly TimeSpan OFFER_DURATION = TimeSpan.FromDays(3);

    public async Task<(int Result, int OfferId)> MakeOfferAsync(
        int furnitureItemId,
        int price,
        CancellationToken ct
    )
    {
        if (price <= 0)
            return (1, 0);

        var inventoryGrain = _grainFactory.GetInventoryGrain(this.GetPrimaryKeyLong());

        var snapshot = await inventoryGrain
            .GetItemSnapshotAsync(new RoomObjectId(furnitureItemId), ct)
            .ConfigureAwait(false);

        if (snapshot is null)
            return (1, 0);

        if (snapshot.RoomId.Value > 0)
            return (1, 0);

        if (!snapshot.Definition.CanSell)
            return (1, 0);

        var removed = await inventoryGrain
            .RemoveFurnitureAsync(new RoomObjectId(furnitureItemId), ct)
            .ConfigureAwait(false);

        if (!removed)
            return (1, 0);

        var furniType =
            snapshot.Definition.ProductType == Turbo.Primitives.Furniture.Enums.ProductType.Wall
                ? 2
                : 1;

        var offer = new MarketplaceOfferEntity
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

        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        dbCtx.MarketplaceOffers.Add(offer);
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        return (0, offer.Id);
    }

    public async Task<bool> CancelOrRedeemOfferAsync(int offerId, CancellationToken ct)
    {
        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var offer = await dbCtx
            .MarketplaceOffers.FirstOrDefaultAsync(
                o => o.Id == offerId && o.SellerEntityId == (int)this.GetPrimaryKeyLong(),
                ct
            )
            .ConfigureAwait(false);

        if (offer is null)
            return false;

        if (offer.State == MarketplaceOfferState.Sold)
            return false;

        offer.State = MarketplaceOfferState.Cancelled;
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        var inventoryGrain = _grainFactory.GetInventoryGrain(this.GetPrimaryKeyLong());
        await inventoryGrain
            .GrantFurnitureDefinitionAsync(offer.FurnitureDefinitionEntityId, offer.ExtraData, ct)
            .ConfigureAwait(false);

        return true;
    }

    public async Task<int> BuyOfferAsync(int offerId, CancellationToken ct)
    {
        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var now = DateTime.UtcNow;
        var offer = await dbCtx
            .MarketplaceOffers.FirstOrDefaultAsync(
                o =>
                    o.Id == offerId && o.State == MarketplaceOfferState.Active && o.ExpiresAt > now,
                ct
            )
            .ConfigureAwait(false);

        if (offer is null)
            return 1;

        var walletGrain = _grainFactory.GetPlayerWalletGrain(this.GetPrimaryKeyLong());
        var debitResult = await walletGrain
            .TryDebitAsync(
                [
                    new WalletDebitRequest
                    {
                        CurrencyKind = new CurrencyKind { CurrencyType = CurrencyType.Credits },
                        Amount = offer.Price,
                    },
                ],
                ct
            )
            .ConfigureAwait(false);

        if (!debitResult.Succeeded)
            return 2;

        var commission = Math.Max(1, offer.Price * COMMISSION_PERCENT / 100);
        offer.State = MarketplaceOfferState.Sold;
        offer.CreditsOwed = offer.Price - commission;
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        var inventoryGrain = _grainFactory.GetInventoryGrain(this.GetPrimaryKeyLong());
        await inventoryGrain
            .GrantFurnitureDefinitionAsync(offer.FurnitureDefinitionEntityId, offer.ExtraData, ct)
            .ConfigureAwait(false);

        return 0;
    }

    public async Task<int> RedeemCreditsAsync(CancellationToken ct)
    {
        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var soldOffers = await dbCtx
            .MarketplaceOffers.Where(o =>
                o.SellerEntityId == (int)this.GetPrimaryKeyLong()
                && o.State == MarketplaceOfferState.Sold
                && o.CreditsOwed > 0
            )
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var totalCredits = soldOffers.Sum(o => o.CreditsOwed);

        if (totalCredits <= 0)
            return 0;

        foreach (var o in soldOffers)
            o.CreditsOwed = 0;

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
        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var now = DateTime.UtcNow;
        var offers = await dbCtx
            .MarketplaceOffers.Where(o =>
                o.SellerEntityId == (int)this.GetPrimaryKeyLong()
                && o.State != MarketplaceOfferState.Cancelled
            )
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var creditsOwed = offers.Sum(o => o.CreditsOwed);

        var snapshots = offers
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
