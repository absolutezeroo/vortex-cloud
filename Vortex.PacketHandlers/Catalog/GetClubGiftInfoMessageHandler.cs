using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Catalog;
using Vortex.Primitives.Catalog.Providers;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Players;

namespace Vortex.PacketHandlers.Catalog;

public class GetClubGiftInfoMessageHandler(
    IGrainFactory grainFactory,
    ICatalogClubGiftProvider clubGiftProvider
) : IMessageHandler<GetClubGiftInfoMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ICatalogClubGiftProvider _clubGiftProvider = clubGiftProvider;

    public async ValueTask HandleAsync(
        GetClubGiftInfoMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        ClubSubscriptionSnapshot sub = await _grainFactory
            .GetPlayerGrain(ctx.PlayerId)
            .GetClubSubscriptionAsync(ct)
            .ConfigureAwait(false);

        IReadOnlyList<CatalogOfferSnapshot> offers = _clubGiftProvider.GetAll();

        DateTime now = DateTime.UtcNow;
        int daysUntilNextGift =
            sub.IsActive && sub.NextGiftAt.HasValue && sub.NextGiftAt.Value > now
                ? (int)Math.Ceiling((sub.NextGiftAt.Value - now).TotalDays)
                : 0;

        List<ClubGiftOfferData> giftData = offers
            .Select(o =>
            {
                bool isVipGift = o.ClubLevel > 1;
                return new ClubGiftOfferData
                {
                    OfferId = o.Id,
                    IsVip = isVipGift,
                    DaysRequired = 0,
                    // Only claimable with an active membership, a gift token in hand, and — for
                    // VIP-only gifts — an active VIP tier.
                    IsSelectable =
                        sub.IsActive && sub.GiftsAvailable > 0 && (!isVipGift || sub.IsVip),
                };
            })
            .ToList();

        await ctx.SendComposerAsync(
                new ClubGiftInfoEventMessageComposer
                {
                    DaysUntilNextGift = daysUntilNextGift,
                    GiftsAvailable = sub.GiftsAvailable,
                    Offers = offers,
                    GiftData = giftData,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
