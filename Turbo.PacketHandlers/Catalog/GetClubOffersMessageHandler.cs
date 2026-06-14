using System;
using System.Collections.Generic;
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

public class GetClubOffersMessageHandler(IGrainFactory grainFactory, ICatalogClubOfferProvider clubOfferProvider)
    : IMessageHandler<GetClubOffersMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ICatalogClubOfferProvider _clubOfferProvider = clubOfferProvider;

    public async ValueTask HandleAsync(
        GetClubOffersMessage message,
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

        var now = DateTime.UtcNow;
        var baseDate = sub.IsActive && sub.ExpiresAt > now ? sub.ExpiresAt : now;

        var personalizedOffers = new List<ClubOffer>();
        foreach (var offer in _clubOfferProvider.GetAll())
        {
            var expiry = baseDate.AddMonths(offer.Months).AddDays(offer.ExtraDays);
            personalizedOffers.Add(offer with
            {
                DaysLeftAfterPurchase = (int)(expiry - now).TotalDays,
                Year = expiry.Year,
                Month = expiry.Month,
                Day = expiry.Day,
            });
        }

        await ctx.SendComposerAsync(
            new HabboClubOffersMessageComposer
            {
                Offers = personalizedOffers,
                Source = message.RequestSource,
            },
            ct
        ).ConfigureAwait(false);
    }
}
