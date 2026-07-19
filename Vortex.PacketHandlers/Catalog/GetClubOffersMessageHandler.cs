using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Catalog;
using Vortex.Primitives.Catalog.Providers;
using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Players;

namespace Vortex.PacketHandlers.Catalog;

public class GetClubOffersMessageHandler(
    IGrainFactory grainFactory,
    ICatalogClubOfferProvider clubOfferProvider
) : IMessageHandler<GetClubOffersMessage>
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
        {
            return;
        }

        ClubSubscriptionSnapshot sub = await _grainFactory
            .GetPlayerGrain(ctx.PlayerId)
            .GetClubSubscriptionAsync(ct)
            .ConfigureAwait(false);

        DateTime now = DateTime.UtcNow;
        DateTime baseDate = sub.IsActive && sub.ExpiresAt > now ? sub.ExpiresAt : now;

        List<ClubOffer> personalizedOffers = new List<ClubOffer>();
        foreach (ClubOffer offer in _clubOfferProvider.GetAll())
        {
            DateTime expiry = baseDate.AddMonths(offer.Months).AddDays(offer.ExtraDays);
            personalizedOffers.Add(
                offer with
                {
                    DaysLeftAfterPurchase = (int)(expiry - now).TotalDays,
                    Year = expiry.Year,
                    Month = expiry.Month,
                    Day = expiry.Day,
                }
            );
        }

        await ctx.SendComposerAsync(
                new HabboClubOffersMessageComposer
                {
                    Offers = personalizedOffers,
                    Source = message.RequestSource,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
