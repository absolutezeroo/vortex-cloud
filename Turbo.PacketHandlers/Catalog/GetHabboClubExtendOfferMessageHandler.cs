using System;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Catalog.Providers;
using Turbo.Primitives.Messages.Incoming.Catalog;
using Turbo.Primitives.Messages.Outgoing.Catalog;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Catalog;

public class GetHabboClubExtendOfferMessageHandler(IGrainFactory grainFactory, ICatalogClubOfferProvider clubOfferProvider)
    : IMessageHandler<GetHabboClubExtendOfferMessage>
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
            return;

        var sub = await _grainFactory
            .GetPlayerGrain(ctx.PlayerId)
            .GetClubSubscriptionAsync(ct)
            .ConfigureAwait(false);

        // Show the 1-month offer matching the player's current level (VIP id=3, Basic id=1)
        var baseOffer = sub.IsVip
            ? _clubOfferProvider.FindById(3)
            : _clubOfferProvider.FindById(1);

        if (baseOffer is null)
            return;

        var now = DateTime.UtcNow;
        var baseDate = sub.IsActive && sub.ExpiresAt > now ? sub.ExpiresAt : now;
        var expiry = baseDate.AddMonths(baseOffer.Months);

        var personalizedOffer = baseOffer with
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
        ).ConfigureAwait(false);
    }
}
