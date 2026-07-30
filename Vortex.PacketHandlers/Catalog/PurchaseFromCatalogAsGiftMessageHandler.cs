using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Catalog.Exceptions;
using Vortex.Messages.Registry;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;

namespace Vortex.PacketHandlers.Catalog;

public class PurchaseFromCatalogAsGiftMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<PurchaseFromCatalogAsGiftMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        PurchaseFromCatalogAsGiftMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || string.IsNullOrWhiteSpace(message.RecieverName))
        {
            return;
        }

        PlayerId? receiverId = await _grainFactory
            .GetPlayerDirectoryGrain()
            .GetPlayerIdAsync(message.RecieverName, ct)
            .ConfigureAwait(false);

        if (receiverId is null)
        {
            await ctx.SendComposerAsync(new GiftReceiverNotFoundEventMessageComposer(), ct)
                .ConfigureAwait(false);

            return;
        }

        try
        {
            // Everything the purchase involves -- offer lookup, the giftable check, the club gate,
            // the HC discount, the debit and its refund, spend tracking, the audit trail -- belongs
            // to the purchase grain. This handler only resolves the recipient and answers the client.
            CatalogOfferSnapshot offer = await _grainFactory
                .GetCatalogPurchaseGrain(ctx.PlayerId)
                .PurchaseOfferAsGiftAsync(
                    CatalogType.Normal,
                    message.OfferCode,
                    message.ExtraParam ?? string.Empty,
                    receiverId.Value,
                    ct
                )
                .ConfigureAwait(false);

            await ctx.SendComposerAsync(new PurchaseOKMessageComposer { Offer = offer }, ct)
                .ConfigureAwait(false);
        }
        catch (CatalogPurchaseException ex)
        {
            await CatalogPurchaseErrorResponder.SendAsync(ctx, ex, ct).ConfigureAwait(false);
        }
    }
}
