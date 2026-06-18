using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Catalog;
using Turbo.Primitives.Catalog.Providers;
using Turbo.Primitives.Messages.Incoming.Catalog;
using Turbo.Primitives.Messages.Outgoing.Catalog;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Catalog;

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
            return;

        var sub = await _grainFactory
            .GetPlayerGrain(ctx.PlayerId)
            .GetClubSubscriptionAsync(ct)
            .ConfigureAwait(false);

        var offers = _clubGiftProvider.GetAll();

        var now = DateTime.UtcNow;
        var daysUntilNextGift =
            sub.IsActive && sub.NextGiftAt.HasValue && sub.NextGiftAt.Value > now
                ? (int)Math.Ceiling((sub.NextGiftAt.Value - now).TotalDays)
                : 0;

        var giftData = offers
            .Select(o =>
            {
                var isVipGift = o.ClubLevel > 1;
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
