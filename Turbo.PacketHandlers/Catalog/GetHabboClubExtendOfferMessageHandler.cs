using System;
using System.Collections.Generic;
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
using Turbo.Primitives.Orleans.Snapshots.Players;

namespace Turbo.PacketHandlers.Catalog;

public class GetHabboClubExtendOfferMessageHandler(
    IGrainFactory grainFactory,
    ICatalogClubOfferProvider clubOfferProvider
) : IMessageHandler<GetHabboClubExtendOfferMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ICatalogClubOfferProvider _clubOfferProvider = clubOfferProvider;

    public async ValueTask HandleAsync(
        GetHabboClubExtendOfferMessage message,
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

        List<ClubOffer> offerPool = _clubOfferProvider
            .GetAll()
            .Where(x => x.IsVip == sub.IsVip)
            .ToList();

        // Keep the semantics of a 1-month renewal offer when possible, and fallback to the
        // first matching tier offer if that data is missing.
        ClubOffer? baseOffer =
            offerPool.FirstOrDefault(x => x.Months == 1) ?? offerPool.FirstOrDefault();

        if (baseOffer is null)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;
        DateTime baseDate = sub.IsActive && sub.ExpiresAt > now ? sub.ExpiresAt : now;
        DateTime expiry = baseDate.AddMonths(baseOffer.Months).AddDays(baseOffer.ExtraDays);

        ClubOffer personalizedOffer = baseOffer with
        {
            DaysLeftAfterPurchase = (int)(expiry - now).TotalDays,
            Year = expiry.Year,
            Month = expiry.Month,
            Day = expiry.Day,
        };

        await ctx.SendComposerAsync(
                new HabboClubExtendOfferMessageComposer
                {
                    Offer = personalizedOffer,
                    OriginalPricePerMonth = baseOffer.PriceCredits,
                    OriginalActivityPointPricePerMonth = baseOffer.PriceActivityPoints,
                    OriginalActivityPointType = baseOffer.PriceActivityPointType,
                    SubscriptionDaysLeft = sub.DaysLeft,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
