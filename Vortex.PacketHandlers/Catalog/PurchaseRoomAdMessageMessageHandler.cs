using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Catalog.Exceptions;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Primitives.Catalog.Grains;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Navigator;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.Catalog;

public class PurchaseRoomAdMessageMessageHandler(
    IGrainFactory grainFactory,
    INavigatorProvider navigatorProvider
) : IMessageHandler<PurchaseRoomAdMessageMessage>
{
    /// <summary>Real Habbo badge granted on a player's first-ever room-ad purchase.</summary>
    private const string RoomAdBadgeCode = "RADZZ";

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

        if (!navigatorProvider.GetFlatCategories().Any(c => c.Id == message.CategoryId))
        {
            await ctx.SendComposerAsync(new PurchaseErrorMessageComposer(), ct)
                .ConfigureAwait(false);
            return;
        }

        IRoomGrain roomGrain = grainFactory.GetRoomGrain(message.FlatId);
        ActionContext actionCtx = ActionContext.CreateForPlayer(ctx.PlayerId, message.FlatId);
        RoomControllerType controllerLevel = await roomGrain
            .GetControllerLevelAsync(actionCtx, ct)
            .ConfigureAwait(false);

        if (controllerLevel < RoomControllerType.Rights)
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

            await grainFactory
                .GetInventoryGrain(ctx.PlayerId)
                .GrantBadgeAsync(RoomAdBadgeCode, ct)
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
