using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using Vortex.Catalog.Exceptions;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Events;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Wallet;

namespace Vortex.Catalog.Grains;

public sealed partial class CatalogPurchaseGrain
{
    public async Task<CatalogOfferSnapshot> PurchaseOfferAsGiftAsync(
        CatalogType catalogType,
        int offerId,
        string extraParam,
        PlayerId receiverId,
        CancellationToken ct
    )
    {
        int buyerId = (int)this.GetPrimaryKeyLong();

        if (receiverId.Value <= 0)
        {
            throw new CatalogPurchaseException(CatalogPurchaseErrorType.PurchaseFailed);
        }

        CatalogSnapshot snapshot = _catalogService.GetCatalogSnapshot(catalogType);

        if (!snapshot.OffersById.TryGetValue(offerId, out CatalogOfferSnapshot? offer))
        {
            throw new CatalogPurchaseException(CatalogPurchaseErrorType.OfferNotFound);
        }

        if (!offer.CanGift)
        {
            throw new CatalogPurchaseException(CatalogPurchaseErrorType.PurchaseFailed);
        }

        // The club gate and the discount follow the payer, not the recipient: gifting is a purchase
        // the buyer makes, so it cannot become a way around a club-only offer.
        int discountPercent = await ResolveClubPricingAsync(offer, ct).ConfigureAwait(true);

        // Gifting has no quantity on the wire -- the client sends one recipient and one offer.
        TryGetDebitRequests(offer, 1, discountPercent, out List<WalletDebitRequest> debitRequests);

        int creditCost = debitRequests
            .Where(r => r.CurrencyKind.CurrencyType == CurrencyType.Credits)
            .Sum(r => r.Amount);

        IPlayerWalletGrain wallet = _grainFactory.GetPlayerWalletGrain(buyerId);

        WalletPurchaseResult<CatalogOfferSnapshot> result = await wallet
            .ExecutePurchaseAsync(
                debitRequests,
                async innerCt =>
                {
                    // Ordered least-to-most reversible, and the notification events moved outside
                    // this compensated scope entirely (see SEC-06): a failure tracking a stat is
                    // harmless to compensate, but a failure *after* the inventory grant would leave
                    // the recipient's gift delivered for free once the wallet refunds the buyer.
                    if (creditCost > 0)
                    {
                        await _grainFactory
                            .GetPlayerGrain(PlayerId.Parse(buyerId))
                            .TrackCreditSpendAsync(creditCost, innerCt)
                            .ConfigureAwait(true);
                    }

                    await _grainFactory
                        .GetInventoryGrain(receiverId.Value)
                        .GrantCatalogOfferAsync(offer, extraParam, 1, innerCt)
                        .ConfigureAwait(true);

                    return offer;
                },
                _logger,
                ct
            )
            .ConfigureAwait(true);

        if (!result.Succeeded)
        {
            throw CreateInsufficientBalanceException(result.Failure);
        }

        await _events
            .PublishAsync(
                new CatalogPurchasedEvent(buyerId, catalogType.ToString(), offerId, 1, creditCost),
                ct
            )
            .ConfigureAwait(true);

        // Emitted on top of the purchase event rather than instead of it: the buyer's spend belongs
        // in the same series as every other purchase, and a gift also has to answer "to whom".
        await _events
            .PublishAsync(
                new CatalogGiftPurchasedEvent(buyerId, receiverId.Value, offerId, creditCost),
                ct
            )
            .ConfigureAwait(true);

        return result.Reward!;
    }
}
