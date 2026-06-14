using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Catalog.Exceptions;
using Turbo.Messages.Registry;
using Turbo.Primitives.Catalog;
using Turbo.Primitives.Catalog.Enums;
using Turbo.Primitives.Catalog.Providers;
using Turbo.Primitives.Messages.Incoming.Catalog;
using Turbo.Primitives.Messages.Outgoing.Catalog;
using Turbo.Primitives.Messages.Outgoing.Handshake;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players.Enums;
using Turbo.Primitives.Players.Enums.Wallet;
using Turbo.Primitives.Players.Wallet;

namespace Turbo.PacketHandlers.Catalog;

public class PurchaseFromCatalogMessageHandler(
    IGrainFactory grainFactory,
    ICatalogService catalogService,
    ICatalogClubOfferProvider clubOfferProvider
) : IMessageHandler<PurchaseFromCatalogMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ICatalogService _catalogService = catalogService;
    private readonly ICatalogClubOfferProvider _clubOfferProvider = clubOfferProvider;

    public async ValueTask HandleAsync(
        PurchaseFromCatalogMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
            return;

        var snapshot = _catalogService.GetCatalogSnapshot(CatalogType.Normal);

        if (snapshot.PagesById.TryGetValue(message.PageId, out var page)
            && page.Layout == CatalogPageLayout.ClubBuy)
        {
            await HandleClubPurchaseAsync(message, ctx, ct);
            return;
        }

        await HandleCatalogPurchaseAsync(message, ctx, ct);
    }

    private async ValueTask HandleClubPurchaseAsync(
        PurchaseFromCatalogMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        var clubOffer = _clubOfferProvider.FindById(message.OfferId);

        if (clubOffer is null)
        {
            await ctx.SendComposerAsync(
                new PurchaseNotAllowedMessageComposer { ErrorType = CatalogPurchaseErrorType.OfferNotFound },
                ct
            ).ConfigureAwait(false);
            return;
        }

        if (clubOffer.PriceCredits > 0)
        {
            var wallet = _grainFactory.GetPlayerWalletGrain(ctx.PlayerId);
            var credits = await wallet
                .GetAmountForCurrencyAsync(new CurrencyKind { CurrencyType = CurrencyType.Credits }, ct)
                .ConfigureAwait(false);

            if (credits < clubOffer.PriceCredits)
            {
                await ctx.SendComposerAsync(
                    new NotEnoughBalanceMessageComposer
                    {
                        NotEnoughCredits = true,
                        NotEnoughActivityPoints = false,
                        ActivityPointType = 0,
                    },
                    ct
                ).ConfigureAwait(false);
                return;
            }
        }

        var playerGrain = _grainFactory.GetPlayerGrain(ctx.PlayerId);
        await playerGrain
            .PurchaseClubAsync(clubOffer.Months, clubOffer.IsVip, clubOffer.PriceCredits, ct)
            .ConfigureAwait(false);

        var sub = await playerGrain.GetClubSubscriptionAsync(ct).ConfigureAwait(false);

        var clubLevel = sub.IsActive
            ? (sub.IsVip ? ClubLevelType.Vip : ClubLevelType.Club)
            : ClubLevelType.None;

        var now = DateTime.UtcNow;
        var baseDate = sub.ExpiresAt > now ? sub.ExpiresAt : now;

        var refreshedOffers = new List<ClubOffer>();
        foreach (var offer in _clubOfferProvider.GetAll())
        {
            var expiry = baseDate.AddMonths(offer.Months).AddDays(offer.ExtraDays);
            refreshedOffers.Add(offer with
            {
                DaysLeftAfterPurchase = (int)(expiry - now).TotalDays,
                Year = expiry.Year,
                Month = expiry.Month,
                Day = expiry.Day,
            });
        }

        await ctx.SendComposerAsync(
            new UserRightsMessage
            {
                ClubLevel = clubLevel,
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
                    PastVipDays = sub.IsVip ? sub.TotalMonths * 31 : 0,
                    MinutesUntilExpiration = (int)(sub.ExpiresAt - DateTime.UtcNow).TotalMinutes,
                    MinutesSinceLastModified = 0,
                },
                ct
            ).ConfigureAwait(false);
        }

        await ctx.SendComposerAsync(
            new HabboClubOffersMessageComposer
            {
                Offers = refreshedOffers,
                Source = ClubOfferRequestSourceType.Unknown,
            },
            ct
        ).ConfigureAwait(false);
    }

    private async ValueTask HandleCatalogPurchaseAsync(
        PurchaseFromCatalogMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        try
        {
            var purchaseGrain = _grainFactory.GetCatalogPurchaseGrain(ctx.PlayerId);
            var offer = await purchaseGrain
                .PurchaseOfferFromCatalogAsync(
                    CatalogType.Normal,
                    message.OfferId,
                    message.ExtraParam ?? string.Empty,
                    message.Quantity,
                    ct
                )
                .ConfigureAwait(false);

            await ctx.SendComposerAsync(new PurchaseOKMessageComposer { Offer = offer }, ct)
                .ConfigureAwait(false);
        }
        catch (CatalogPurchaseException ex)
        {
            if (ex.BalanceFailure is not null)
            {
                await ctx.SendComposerAsync(
                        new NotEnoughBalanceMessageComposer
                        {
                            NotEnoughCredits = ex.BalanceFailure.NotEnoughCredits,
                            NotEnoughActivityPoints = ex.BalanceFailure.NotEnoughActivityPoints,
                            ActivityPointType = ex.BalanceFailure.ActivityPointType,
                        },
                        ct
                    )
                    .ConfigureAwait(false);
                return;
            }

            await ctx.SendComposerAsync(
                    new PurchaseNotAllowedMessageComposer { ErrorType = ex.ErrorType },
                    ct
                )
                .ConfigureAwait(false);
        }
    }
}
