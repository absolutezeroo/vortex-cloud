using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Catalog.Exceptions;
using Turbo.Primitives.Catalog;
using Turbo.Primitives.Catalog.Enums;
using Turbo.Primitives.Catalog.Grains;
using Turbo.Primitives.Catalog.Snapshots;
using Turbo.Primitives.Events;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Players;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Enums.Wallet;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Players.Wallet;
using Turbo.Primitives.Rooms;

namespace Turbo.Catalog.Grains;

public sealed partial class CatalogPurchaseGrain(
    IGrainFactory grainFactory,
    ICatalogService catalogService,
    IEventPublisher events,
    IRoomAdvertisementService roomAdvertisements,
    ILogger<CatalogPurchaseGrain> logger
) : Grain, ICatalogPurchaseGrain
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ICatalogService _catalogService = catalogService;
    private readonly IEventPublisher _events = events;
    private readonly IRoomAdvertisementService _roomAdvertisements = roomAdvertisements;
    private readonly ILogger<CatalogPurchaseGrain> _logger = logger;

    public async Task<CatalogOfferSnapshot> PurchaseOfferFromCatalogAsync(
        CatalogType catalogType,
        int offerId,
        string extraParam,
        int quantity,
        CancellationToken ct
    )
    {
        quantity = Math.Max(1, quantity);

        CatalogSnapshot snapshot = _catalogService.GetCatalogSnapshot(catalogType);

        if (!snapshot.OffersById.TryGetValue(offerId, out CatalogOfferSnapshot? offer))
        {
            throw new CatalogPurchaseException(CatalogPurchaseErrorType.OfferNotFound);
        }

        bool isHabboClub = false;

        // Resolve membership once when the offer either gates on club level or carries an HC discount.
        if (offer.ClubLevel > 0 || offer.DiscountPercent > 0)
        {
            ClubSubscriptionSnapshot sub = await _grainFactory
                .GetPlayerGrain(PlayerId.Parse((int)this.GetPrimaryKeyLong()))
                .GetClubSubscriptionAsync(ct)
                .ConfigureAwait(false);

            isHabboClub = sub.IsActive;

            int playerClubLevel = sub.IsActive ? (sub.IsVip ? 2 : 1) : 0;

            if (playerClubLevel < offer.ClubLevel)
            {
                throw new CatalogPurchaseException(CatalogPurchaseErrorType.RequiresHabboClub);
            }
        }

        // HC members get the offer's configured discount off the credit cost (0 = no discount).
        int discountPercent = isHabboClub ? Math.Clamp(offer.DiscountPercent, 0, 100) : 0;

        TryGetDebitRequests(
            offer,
            quantity,
            discountPercent,
            out List<WalletDebitRequest> debitRequests
        );

        int creditCost = debitRequests
            .Where(r => r.CurrencyKind.CurrencyType == CurrencyType.Credits)
            .Sum(r => r.Amount);

        IPlayerWalletGrain wallet = _grainFactory.GetPlayerWalletGrain(
            (int)this.GetPrimaryKeyLong()
        );

        WalletPurchaseResult<CatalogOfferSnapshot> result = await wallet
            .ExecutePurchaseAsync(
                debitRequests,
                async innerCt =>
                {
                    await _grainFactory
                        .GetInventoryGrain((int)this.GetPrimaryKeyLong())
                        .GrantCatalogOfferAsync(offer, extraParam, quantity, innerCt)
                        .ConfigureAwait(false);

                    if (creditCost > 0)
                    {
                        await _grainFactory
                            .GetPlayerGrain(PlayerId.Parse((int)this.GetPrimaryKeyLong()))
                            .TrackCreditSpendAsync(creditCost, innerCt)
                            .ConfigureAwait(false);
                    }

                    await _events
                        .PublishAsync(
                            new CatalogPurchasedEvent(
                                (int)this.GetPrimaryKeyLong(),
                                catalogType.ToString(),
                                offerId,
                                quantity,
                                creditCost
                            ),
                            innerCt
                        )
                        .ConfigureAwait(false);

                    return offer;
                },
                _logger,
                ct
            )
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            throw CreateInsufficientBalanceException(result.Failure);
        }

        return result.Reward!;
    }

    private bool TryGetDebitRequests(
        CatalogOfferSnapshot offer,
        int quantity,
        int discountPercent,
        out List<WalletDebitRequest> requests
    )
    {
        requests = [];

        if (offer.CostCredits > 0)
        {
            int creditAmount = offer.CostCredits * quantity;

            if (discountPercent > 0)
            {
                creditAmount -= (int)(creditAmount * (discountPercent / 100.0));
            }

            if (creditAmount > 0)
            {
                requests.Add(
                    new WalletDebitRequest
                    {
                        CurrencyKind = new CurrencyKind { CurrencyType = CurrencyType.Credits },
                        Amount = creditAmount,
                    }
                );
            }
        }

        if (offer.CostSilver > 0)
        {
            requests.Add(
                new WalletDebitRequest
                {
                    CurrencyKind = new CurrencyKind { CurrencyType = CurrencyType.Silver },
                    Amount = offer.CostSilver * quantity,
                }
            );
        }

        if (offer.CostCurrency > 0)
        {
            requests.Add(
                new WalletDebitRequest
                {
                    CurrencyKind = new CurrencyKind
                    {
                        CurrencyType = CurrencyType.ActivityPoints,
                        ActivityPointType = offer.CurrencyTypeId,
                    },
                    Amount = offer.CostCurrency * quantity,
                }
            );
        }

        return true;
    }

    private static CatalogPurchaseException CreateInsufficientBalanceException(
        WalletDebitFailure? failure
    )
    {
        if (failure is null)
        {
            return new CatalogPurchaseException(CatalogPurchaseErrorType.PurchaseFailed);
        }

        if (failure.CurrencyKind.CurrencyType == CurrencyType.Credits)
        {
            return new CatalogPurchaseException(
                CatalogPurchaseErrorType.NotEnoughCredits,
                new CatalogBalanceFailure
                {
                    NotEnoughCredits = true,
                    NotEnoughActivityPoints = false,
                    ActivityPointType = 0,
                }
            );
        }

        if (failure.CurrencyKind.CurrencyType == CurrencyType.ActivityPoints)
        {
            return new CatalogPurchaseException(
                CatalogPurchaseErrorType.NotEnoughActivityPoints,
                new CatalogBalanceFailure
                {
                    NotEnoughCredits = false,
                    NotEnoughActivityPoints = true,
                    ActivityPointType = failure.CurrencyKind.ActivityPointType ?? -1,
                }
            );
        }

        return new CatalogPurchaseException(CatalogPurchaseErrorType.PurchaseFailed);
    }
}
