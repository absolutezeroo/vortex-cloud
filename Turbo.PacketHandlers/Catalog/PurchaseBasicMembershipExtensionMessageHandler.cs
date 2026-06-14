using System;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Catalog.Providers;
using Turbo.Primitives.Messages.Incoming.Catalog;
using Turbo.Primitives.Messages.Outgoing.Handshake;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players.Enums;

namespace Turbo.PacketHandlers.Catalog;

public class PurchaseBasicMembershipExtensionMessageHandler(IGrainFactory grainFactory, ICatalogClubOfferProvider clubOfferProvider)
    : IMessageHandler<PurchaseBasicMembershipExtensionMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ICatalogClubOfferProvider _clubOfferProvider = clubOfferProvider;

    public async ValueTask HandleAsync(
        PurchaseBasicMembershipExtensionMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
            return;

        var offer = _clubOfferProvider.FindById(message.OfferId);
        if (offer is null || offer.IsVip)
            return;

        var playerGrain = _grainFactory.GetPlayerGrain(ctx.PlayerId);

        await playerGrain.PurchaseClubAsync(offer.Months, false, offer.PriceCredits, ct)
            .ConfigureAwait(false);

        var sub = await playerGrain.GetClubSubscriptionAsync(ct).ConfigureAwait(false);

        await ctx.SendComposerAsync(
            new UserRightsMessage
            {
                ClubLevel = ClubLevelType.Club,
                SecurityLevel = SecurityLevelType.None,
                IsAmbassador = false,
            },
            ct
        ).ConfigureAwait(false);

        if (sub.IsActive)
        {
            var daysLeft = sub.DaysLeft;
            var rem = daysLeft % 31;
            await ctx.SendComposerAsync(
                new ScrSendUserInfoMessageComposer
                {
                    ProductName = "habbo_club",
                    DaysToPeriodEnd = rem == 0 ? 31 : rem,
                    MemberPeriods = sub.TotalMonths,
                    PeriodsSubscribedAhead = daysLeft / 31 - (rem == 0 ? 1 : 0),
                    ResponseType = 2,
                    HasEverBeenMember = sub.TotalMonths > 0 || sub.IsActive,
                    IsVIP = sub.IsVip,
                    PastClubDays = sub.TotalMonths * 31,
                    PastVipDays = 0,
                    MinutesUntilExpiration = (int)(sub.ExpiresAt - DateTime.UtcNow).TotalMinutes,
                    MinutesSinceLastModified = 0,
                },
                ct
            ).ConfigureAwait(false);
        }
    }
}
