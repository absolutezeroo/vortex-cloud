using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Marketplace;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Inventory.Grains;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Marketplace.Grains;
using Vortex.Primitives.Marketplace.Providers;
using Vortex.Primitives.Marketplace.Snapshots;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Wallet;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Marketplace.Grains;

public sealed class MarketplacePurchaseGrain(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    IGrainFactory grainFactory,
    IMarketplaceSettingsProvider settingsProvider,
    ILogger<MarketplacePurchaseGrain> logger
) : Grain, IMarketplacePurchaseGrain
{
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IMarketplaceSettingsProvider _settingsProvider = settingsProvider;
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
            .ConfigureAwait(true);

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
            .ConfigureAwait(true);

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
            ExpiresAt = DateTime.UtcNow.AddSeconds(
                _settingsProvider.GetSettings().OfferDurationSeconds
            ),
        };

        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);
        dbCtx.MarketplaceOffers.Add(offer);
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        return (0, offer.Id);
    }

    public async Task<bool> CancelOrRedeemOfferAsync(int offerId, CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        MarketplaceOfferEntity? offer = await dbCtx
            .MarketplaceOffers.FirstOrDefaultAsync(
                o => o.Id == offerId && o.SellerEntityId == (int)this.GetPrimaryKeyLong(),
                ct
            )
            .ConfigureAwait(true);

        if (offer is null)
        {
            return false;
        }

        if (offer.State == MarketplaceOfferState.Sold)
        {
            return false;
        }

        offer.State = MarketplaceOfferState.Cancelled;
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        IInventoryGrain inventoryGrain = _grainFactory.GetInventoryGrain(this.GetPrimaryKeyLong());
        await inventoryGrain
            .GrantFurnitureDefinitionAsync(offer.FurnitureDefinitionEntityId, offer.ExtraData, ct)
            .ConfigureAwait(true);

        return true;
    }

    public async Task<int> BuyOfferAsync(int offerId, CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        DateTime now = DateTime.UtcNow;
        MarketplaceOfferEntity? offer = await dbCtx
            .MarketplaceOffers.FirstOrDefaultAsync(
                o =>
                    o.Id == offerId && o.State == MarketplaceOfferState.Active && o.ExpiresAt > now,
                ct
            )
            .ConfigureAwait(true);

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
                    int commission = Math.Max(
                        1,
                        offer.Price * _settingsProvider.GetSettings().CommissionPercent / 100
                    );
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
                        .ConfigureAwait(true);

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
                            .ConfigureAwait(true);
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
                                .ConfigureAwait(true);
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
            .ConfigureAwait(true);

        return result.Succeeded ? 0 : 2;
    }

    public async Task<int> RedeemCreditsAsync(CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        List<MarketplaceOfferEntity> soldOffers = await dbCtx
            .MarketplaceOffers.Where(o =>
                o.SellerEntityId == (int)this.GetPrimaryKeyLong()
                && o.State == MarketplaceOfferState.Sold
                && o.CreditsOwed > 0
            )
            .ToListAsync(ct)
            .ConfigureAwait(true);

        int totalCredits = soldOffers.Sum(o => o.CreditsOwed);

        if (totalCredits <= 0)
        {
            return 0;
        }

        foreach (MarketplaceOfferEntity o in soldOffers)
        {
            o.CreditsOwed = 0;
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        await _grainFactory
            .GetPlayerWalletGrain(this.GetPrimaryKeyLong())
            .GrantCreditsAsync(totalCredits, ct)
            .ConfigureAwait(true);

        return totalCredits;
    }

    public async Task<(int CreditsOwed, List<MarketplaceOfferSnapshot> Offers)> GetOwnOffersAsync(
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        DateTime now = DateTime.UtcNow;
        List<MarketplaceOfferEntity> offers = await dbCtx
            .MarketplaceOffers.Where(o =>
                o.SellerEntityId == (int)this.GetPrimaryKeyLong()
                && o.State != MarketplaceOfferState.Cancelled
            )
            .ToListAsync(ct)
            .ConfigureAwait(true);

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
