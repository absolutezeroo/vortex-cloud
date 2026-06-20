using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Catalog.Providers;
using Turbo.Primitives.Catalog.Snapshots;
using Turbo.Primitives.Grains.Players;
using Turbo.Primitives.Messages.Incoming.Catalog;
using Turbo.Primitives.Messages.Outgoing.Catalog;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Players;

namespace Turbo.PacketHandlers.Catalog;

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
