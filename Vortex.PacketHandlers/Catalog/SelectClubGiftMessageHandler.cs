using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Catalog.Providers;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Players.Grains;

namespace Vortex.PacketHandlers.Catalog;

public class SelectClubGiftMessageHandler(
    IGrainFactory grainFactory,
    ICatalogClubGiftProvider clubGiftProvider
) : IMessageHandler<SelectClubGiftMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ICatalogClubGiftProvider _clubGiftProvider = clubGiftProvider;

    public async ValueTask HandleAsync(
        SelectClubGiftMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || string.IsNullOrEmpty(message.ProductCode))
        {
            return;
        }

        CatalogOfferSnapshot? offer = _clubGiftProvider.FindByProductCode(message.ProductCode);

        if (offer is null)
        {
            return;
        }

        IPlayerGrain playerGrain = _grainFactory.GetPlayerGrain(ctx.PlayerId);

        ClubSubscriptionSnapshot sub = await playerGrain
            .GetClubSubscriptionAsync(ct)
            .ConfigureAwait(false);

        // A VIP-only gift cannot be claimed without an active VIP membership.
        if (!sub.IsActive || (offer.ClubLevel > 1 && !sub.IsVip))
        {
            return;
        }

        bool consumed = await playerGrain
            .TryConsumeClubGiftAsync(message.ProductCode, ct)
            .ConfigureAwait(false);

        if (!consumed)
        {
            return;
        }

        await _grainFactory
            .GetInventoryGrain(ctx.PlayerId)
            .GrantCatalogOfferAsync(offer, string.Empty, 1, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new ClubGiftSelectedEventMessageComposer
                {
                    ProductCode = message.ProductCode,
                    Products = offer.Products,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
