using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Catalog;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Messages.Outgoing.Catalog;

namespace Vortex.PacketHandlers.Catalog;

public class GetIsOfferGiftableMessageHandler(ICatalogService catalogService)
    : IMessageHandler<GetIsOfferGiftableMessage>
{
    private readonly ICatalogService _catalogService = catalogService;

    public async ValueTask HandleAsync(
        GetIsOfferGiftableMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        CatalogSnapshot snapshot = _catalogService.GetCatalogSnapshot(CatalogType.Normal);

        bool isGiftable =
            snapshot.OffersById.TryGetValue(message.OfferId, out CatalogOfferSnapshot? offer)
            && offer.CanGift;

        await ctx.SendComposerAsync(
                new IsOfferGiftableEventMessageComposer
                {
                    OfferId = message.OfferId,
                    IsGiftable = isGiftable,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
