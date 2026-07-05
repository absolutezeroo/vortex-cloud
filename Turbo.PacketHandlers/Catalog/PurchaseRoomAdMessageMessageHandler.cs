using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Catalog.Exceptions;
using Turbo.Messages.Registry;
using Turbo.Primitives.Catalog.Grains;
using Turbo.Primitives.Catalog.Snapshots;
using Turbo.Primitives.Messages.Incoming.Catalog;
using Turbo.Primitives.Messages.Outgoing.Catalog;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Room;

namespace Turbo.PacketHandlers.Catalog;

public class PurchaseRoomAdMessageMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<PurchaseRoomAdMessageMessage>
{
    public async ValueTask HandleAsync(
        PurchaseRoomAdMessageMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.FlatId <= 0 || string.IsNullOrEmpty(message.Name))
        {
            return;
        }

        RoomSnapshot roomSnapshot = await grainFactory
            .GetRoomGrain(message.FlatId)
            .GetSnapshotAsync()
            .ConfigureAwait(false);

        if (roomSnapshot.OwnerId != ctx.PlayerId)
        {
            await ctx.SendComposerAsync(new PurchaseErrorMessageComposer(), ct)
                .ConfigureAwait(false);
            return;
        }

        try
        {
            ICatalogPurchaseGrain purchaseGrain = grainFactory.GetCatalogPurchaseGrain(
                ctx.PlayerId
            );

            CatalogOfferSnapshot offer = await purchaseGrain
                .PurchaseRoomAdAsync(
                    message.OfferId,
                    message.FlatId,
                    message.Name,
                    message.Description,
                    message.Extended,
                    message.CategoryId,
                    ct
                )
                .ConfigureAwait(false);

            await ctx.SendComposerAsync(new PurchaseOKMessageComposer { Offer = offer }, ct)
                .ConfigureAwait(false);
        }
        catch (CatalogPurchaseException ex)
        {
            if (ex.BalanceFailure is not null)
            {
                await ctx.SendComposerAsync(
                        new NotEnoughBalanceMessageComposer
                        {
                            NotEnoughCredits = ex.BalanceFailure.NotEnoughCredits,
                            NotEnoughActivityPoints = ex.BalanceFailure.NotEnoughActivityPoints,
                            ActivityPointType = ex.BalanceFailure.ActivityPointType,
                        },
                        ct
                    )
                    .ConfigureAwait(false);
                return;
            }

            await ctx.SendComposerAsync(new PurchaseErrorMessageComposer(), ct)
                .ConfigureAwait(false);
        }
    }
}
