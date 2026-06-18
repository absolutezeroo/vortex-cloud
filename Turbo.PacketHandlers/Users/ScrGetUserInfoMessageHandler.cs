using System;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Users;
using Turbo.Primitives.Messages.Outgoing.Preferences;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Players;
using Turbo.Primitives.Players.Enums;

namespace Turbo.PacketHandlers.Users;

public class ScrGetUserInfoMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<ScrGetUserInfoMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        ScrGetUserInfoMessage message,
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

        await ctx.SendComposerAsync(BuildScrSendUserInfo(message.ProductName, sub), ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new AccountPreferencesEventMessageComposer
                {
                    UIVolume = 0,
                    FurniVolume = 0,
                    TraxVolume = 0,
                    FreeFlowChatDisabled = false,
                    RoomInvitesIgnored = false,
                    RoomCameraFollowDisabled = false,
                    UIFlags = UIFlags.FriendBarExpanded | UIFlags.RoomToolsExpanded,
                    PreferedChatStyle = 1,
                    WiredMenuButton = false,
                    WiredInspectButton = false,
                    PlayTestMode = false,
                    VariableSyntaxMode = 1,
                },
                ct
            )
            .ConfigureAwait(false);
    }

    private static ScrSendUserInfoMessageComposer BuildScrSendUserInfo(
        string productName,
        ClubSubscriptionSnapshot sub
    )
    {
        var daysLeft = sub.DaysLeft;
        var rem = daysLeft % 31;
        return new ScrSendUserInfoMessageComposer
        {
            ProductName = productName,
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
