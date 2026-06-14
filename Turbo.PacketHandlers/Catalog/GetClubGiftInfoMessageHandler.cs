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
            .Select(o => new ClubGiftOfferData
            {
                OfferId = o.Id,
                IsVip = o.ClubLevel > 1,
                DaysRequired = 0,
                IsSelectable = sub.GiftsAvailable > 0,
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
