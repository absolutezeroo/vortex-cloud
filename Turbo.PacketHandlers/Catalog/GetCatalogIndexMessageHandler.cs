using System;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Catalog;
using Turbo.Primitives.Catalog.Snapshots;
using Turbo.Primitives.Messages.Incoming.Catalog;
using Turbo.Primitives.Messages.Outgoing.Catalog;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Players;

namespace Turbo.PacketHandlers.Catalog;

public class GetCatalogIndexMessageHandler(
    ICatalogService catalogService,
    IGrainFactory grainFactory
) : IMessageHandler<GetCatalogIndexMessage>
{
    private readonly ICatalogService _catalogService = catalogService;
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetCatalogIndexMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        try
        {
            CatalogSnapshot snapshot = _catalogService.GetCatalogSnapshot(message.CatalogType);

            await ctx.SendComposerAsync(new CatalogIndexMessageComposer { Catalog = snapshot }, ct)
                .ConfigureAwait(false);

            if (ctx.PlayerId > 0)
            {
                ClubSubscriptionSnapshot sub = await _grainFactory
                    .GetPlayerGrain(ctx.PlayerId)
                    .GetClubSubscriptionAsync(ct)
                    .ConfigureAwait(false);

                await ctx.SendComposerAsync(BuildScrSendUserInfo(sub), ct).ConfigureAwait(false);
            }
        }
        catch (Exception) { }
    }

    private static ScrSendUserInfoMessageComposer BuildScrSendUserInfo(ClubSubscriptionSnapshot sub)
    {
        int daysLeft = sub.DaysLeft;
        int rem = daysLeft % 31;
        return new ScrSendUserInfoMessageComposer
        {
            ProductName = "habbo_club",
            DaysToPeriodEnd = sub.IsActive ? (rem == 0 ? 31 : rem) : 0,
            MemberPeriods = sub.TotalMonths,
            PeriodsSubscribedAhead = sub.IsActive ? daysLeft / 31 - (rem == 0 ? 1 : 0) : 0,
            ResponseType = sub.IsActive ? 2 : 0,
            HasEverBeenMember = sub.TotalMonths > 0 || sub.IsActive,
            IsVIP = sub.IsVip,
            PastClubDays = sub.PastClubDays,
            PastVipDays = sub.PastVipDays,
            MinutesUntilExpiration = sub.IsActive
                ? (int)(sub.ExpiresAt - DateTime.UtcNow).TotalMinutes
                : 0,
            MinutesSinceLastModified = 0,
        };
    }
}
