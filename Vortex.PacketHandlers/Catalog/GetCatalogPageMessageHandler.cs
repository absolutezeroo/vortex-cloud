using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Messages.Registry;
using Vortex.Primitives.Catalog;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Messages.Outgoing.Catalog;

namespace Vortex.PacketHandlers.Catalog;

public class GetCatalogPageMessageHandler(
    ICatalogService catalogService,
    ILogger<GetCatalogPageMessageHandler> logger
) : IMessageHandler<GetCatalogPageMessage>
{
    private readonly ICatalogService _catalogService = catalogService;
    private readonly ILogger<GetCatalogPageMessageHandler> _logger = logger;

    public async ValueTask HandleAsync(
        GetCatalogPageMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        try
        {
            CatalogSnapshot snapshot = _catalogService.GetCatalogSnapshot(message.CatalogType);

            if (!snapshot.PagesById.TryGetValue(message.PageId, out CatalogPageSnapshot? page))
            {
                return;
            }

            List<CatalogOfferSnapshot> offers = new List<CatalogOfferSnapshot>();
            Dictionary<int, ImmutableArray<CatalogProductSnapshot>> offerProducts =
                new Dictionary<int, ImmutableArray<CatalogProductSnapshot>>();

            foreach (int offerId in page.OfferIds)
            {
                if (snapshot.OffersById.TryGetValue(offerId, out CatalogOfferSnapshot? offer))
                {
                    offers.Add(offer);
                }
            }

            foreach (CatalogOfferSnapshot offer in offers)
            {
                if (
                    !snapshot.OfferProductIds.TryGetValue(
                        offer.Id,
                        out ImmutableArray<int> productIds
                    )
                )
                {
                    continue;
                }

                List<CatalogProductSnapshot> products = new List<CatalogProductSnapshot>();

                foreach (int productId in productIds)
                {
                    if (
                        snapshot.ProductsById.TryGetValue(
                            productId,
                            out CatalogProductSnapshot? product
                        )
                    )
                    {
                        products.Add(product);
                    }
                }

                offerProducts[offer.Id] = [.. products];
            }

            await ctx.SendComposerAsync(
                    new CatalogPageMessageComposer
                    {
                        CatalogType = snapshot.CatalogType,
                        Page = page,
                        Offers = [.. offers],
                        OfferProducts = offerProducts.ToImmutableDictionary(),
                        OfferId = message.OfferId,
                        AcceptSeasonCurrencyAsCredits = false,
                        FrontPageItems = snapshot.FrontPageItems,
                    },
                    ct
                )
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to serve catalog page {PageId} for type {CatalogType}",
                message.PageId,
                message.CatalogType
            );
        }
    }
}
