using System;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Catalog;
using Turbo.Primitives.Catalog.Providers;
using Turbo.Primitives.Messages.Incoming.Catalog;
using Turbo.Primitives.Messages.Outgoing.Catalog;
using Turbo.Primitives.Messages.Outgoing.Handshake;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Players;
using Turbo.Primitives.Players.Enums;
using Turbo.Primitives.Players.Grains;

namespace Turbo.PacketHandlers.Catalog;

public class PurchaseVipMembershipExtensionMessageHandler(
    IGrainFactory grainFactory,
    ICatalogClubOfferProvider clubOfferProvider
) : IMessageHandler<PurchaseVipMembershipExtensionMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ICatalogClubOfferProvider _clubOfferProvider = clubOfferProvider;

    public async ValueTask HandleAsync(
        PurchaseVipMembershipExtensionMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        ClubOffer? offer = _clubOfferProvider.FindById(message.OfferId);
        if (offer is null || !offer.IsVip)
        {
            return;
        }

        IPlayerGrain playerGrain = _grainFactory.GetPlayerGrain(ctx.PlayerId);

        ClubPurchaseResult result = await playerGrain
            .PurchaseClubAsync(offer.Months, true, offer.PriceCredits, ct)
            .ConfigureAwait(false);

        if (result != ClubPurchaseResult.Success)
        {
            await ctx.SendComposerAsync(new PurchaseErrorMessageComposer(), ct)
                .ConfigureAwait(false);
            return;
        }

        ClubSubscriptionSnapshot sub = await playerGrain
            .GetClubSubscriptionAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new UserRightsMessage
                {
                    ClubLevel = ClubLevelType.Vip,
                    SecurityLevel = SecurityLevelType.None,
                    IsAmbassador = false,
                },
                ct
            )
            .ConfigureAwait(false);

        if (sub.IsActive)
        {
            int daysLeft = sub.DaysLeft;
            int rem = daysLeft % 31;
            await ctx.SendComposerAsync(
                    new ScrSendUserInfoMessageComposer
                    {
                        ProductName = "habbo_club",
                        DaysToPeriodEnd = rem == 0 ? 31 : rem,
                        MemberPeriods = sub.TotalMonths,
                        PeriodsSubscribedAhead = daysLeft / 31 - (rem == 0 ? 1 : 0),
                        ResponseType = 2,
                        HasEverBeenMember = sub.TotalMonths > 0 || sub.IsActive,
                        IsVIP = true,
                        PastClubDays = sub.PastClubDays,
                        PastVipDays = sub.PastVipDays,
                        MinutesUntilExpiration = (int)
                            (sub.ExpiresAt - DateTime.UtcNow).TotalMinutes,
                        MinutesSinceLastModified = 0,
                    },
                    ct
                )
                .ConfigureAwait(false);
        }
    }
}
