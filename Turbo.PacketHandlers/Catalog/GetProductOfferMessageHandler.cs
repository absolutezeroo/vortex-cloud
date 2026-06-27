using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Turbo.Messages.Registry;
using Turbo.Primitives.Catalog;
using Turbo.Primitives.Catalog.Enums;
using Turbo.Primitives.Catalog.Snapshots;
using Turbo.Primitives.Messages.Incoming.Catalog;
using Turbo.Primitives.Messages.Outgoing.Catalog;

namespace Turbo.PacketHandlers.Catalog;

public class GetProductOfferMessageHandler(
    ICatalogService catalogService,
    ILogger<GetProductOfferMessageHandler> logger
) : IMessageHandler<GetProductOfferMessage>
{
    private readonly ICatalogService _catalogService = catalogService;
    private readonly ILogger<GetProductOfferMessageHandler> _logger = logger;

    public async ValueTask HandleAsync(
        GetProductOfferMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        try
        {
            CatalogSnapshot snapshot = _catalogService.GetCatalogSnapshot(CatalogType.Normal);

            if (!snapshot.OffersById.TryGetValue(message.OfferId, out CatalogOfferSnapshot? offer))
            {
                return;
            }

            await ctx.SendComposerAsync(new ProductOfferEventMessageComposer { Offer = offer }, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serve product offer {OfferId}", message.OfferId);
        }
    }
}
