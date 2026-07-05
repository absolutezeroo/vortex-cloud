using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Catalog;
using Turbo.Primitives.Catalog.Enums;
using Turbo.Primitives.Catalog.Snapshots;
using Turbo.Primitives.Messages.Incoming.Catalog;
using Turbo.Primitives.Messages.Outgoing.Catalog;

namespace Turbo.PacketHandlers.Catalog;

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
