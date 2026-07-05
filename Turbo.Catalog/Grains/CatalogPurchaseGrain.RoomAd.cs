using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using Turbo.Catalog.Exceptions;
using Turbo.Primitives.Catalog.Enums;
using Turbo.Primitives.Catalog.Snapshots;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players.Enums.Wallet;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Players.Wallet;

namespace Turbo.Catalog.Grains;

public sealed partial class CatalogPurchaseGrain
{
    public async Task<CatalogOfferSnapshot> PurchaseRoomAdAsync(
        int offerId,
        int roomId,
        string name,
        string? description,
        bool extended,
        int categoryId,
        CancellationToken ct
    )
    {
        CatalogSnapshot snapshot = _catalogService.GetCatalogSnapshot(CatalogType.Normal);

        if (!snapshot.OffersById.TryGetValue(offerId, out CatalogOfferSnapshot? offer))
        {
            throw new CatalogPurchaseException(CatalogPurchaseErrorType.OfferNotFound);
        }

        // Room-ad products encode their duration in days via the shared Quantity field, the same
        // way club-buy products reuse it for months -- "Extended" doubles it rather than needing a
        // second configured value per offer.
        int durationDays = snapshot
            .OfferProductIds.GetValueOrDefault(offerId, [])
            .Select(pid => snapshot.ProductsById.GetValueOrDefault(pid)?.Quantity ?? 0)
            .DefaultIfEmpty(1)
            .Max();
        durationDays = Math.Max(1, durationDays) * (extended ? 2 : 1);

        TryGetDebitRequests(offer, 1, 0, out List<WalletDebitRequest> debitRequests);

        IPlayerWalletGrain wallet = _grainFactory.GetPlayerWalletGrain(
            (int)this.GetPrimaryKeyLong()
        );

        WalletPurchaseResult<CatalogOfferSnapshot> result = await wallet
            .ExecutePurchaseAsync(
                debitRequests,
                async innerCt =>
                {
                    await _roomAdvertisements
                        .CreateAsync(
                            roomId,
                            name,
                            description,
                            categoryId,
                            extended,
                            DateTime.UtcNow.AddDays(durationDays),
                            innerCt
                        )
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

        return result.Reward!;
    }
}
