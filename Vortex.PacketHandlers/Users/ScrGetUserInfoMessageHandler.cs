using System;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Messages.Outgoing.Preferences;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Players.Enums;
using Vortex.Primitives.Players.Grains;

namespace Vortex.PacketHandlers.Users;

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
        {
            return;
        }

        IPlayerGrain player = _grainFactory.GetPlayerGrain(ctx.PlayerId);

        ClubSubscriptionSnapshot sub = await player
            .GetClubSubscriptionAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(BuildScrSendUserInfo(message.ProductName, sub), ct)
            .ConfigureAwait(false);

        PlayerWiredPreferencesSnapshot wiredPrefs = await player
            .GetWiredPreferencesAsync(ct)
            .ConfigureAwait(false);

        // Reflect the player's saved chat-bubble style so the settings UI shows their selection
        // (in-memory read on the already-activated grain — no extra DB query).
        int preferedChatStyle = await player.GetChatStylePreferenceAsync(ct).ConfigureAwait(false);

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
                    PreferedChatStyle = preferedChatStyle,
                    WiredMenuButton = wiredPrefs.WiredMenuButton,
                    WiredInspectButton = wiredPrefs.WiredInspectButton,
                    PlayTestMode = wiredPrefs.PlayTestMode,
                    VariableSyntaxMode = 1,
                    WiredWhisperDisabled = wiredPrefs.WiredWhisperDisabled,
                    ShowAllNotifications = wiredPrefs.ShowAllNotifications,
                    UiStyle = wiredPrefs.UiStyle,
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
        int daysLeft = sub.DaysLeft;
        int rem = daysLeft % 31;
        return new ScrSendUserInfoMessageComposer
        {
            ProductName = productName,
            DaysToPeriodEnd = sub.IsActive ? (rem == 0 ? 31 : rem) : 0,
            MemberPeriods = sub.TotalMonths,
            PeriodsSubscribedAhead = sub.IsActive ? daysLeft / 31 - (rem == 0 ? 1 : 0) : 0,
            ResponseType = sub.IsActive ? 2 : 0,
            HasEverBeenMember = sub.TotalMonths > 0 || sub.IsActive,
            IsVIP = sub.IsActive,
            PastClubDays = sub.PastClubDays,
            PastVipDays = sub.PastVipDays,
            MinutesUntilExpiration = sub.IsActive
                ? (int)(sub.ExpiresAt - DateTime.UtcNow).TotalMinutes
                : 0,
            MinutesSinceLastModified = 0,
        };
    }
}
