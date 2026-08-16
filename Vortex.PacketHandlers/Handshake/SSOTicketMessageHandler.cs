using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Authentication;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Messages.Incoming.Handshake;
using Vortex.Primitives.Messages.Outgoing.Availability;
using Vortex.Primitives.Messages.Outgoing.Callforhelp;
using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Messages.Outgoing.Handshake;
using Vortex.Primitives.Messages.Outgoing.Inventory.Achievements;
using Vortex.Primitives.Messages.Outgoing.Inventory.Avatareffect;
using Vortex.Primitives.Messages.Outgoing.Inventory.Clothing;
using Vortex.Primitives.Messages.Outgoing.Inventory.Purse;
using Vortex.Primitives.Messages.Outgoing.Moderation;
using Vortex.Primitives.Messages.Outgoing.Mysterybox;
using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Messages.Outgoing.Notifications;
using Vortex.Primitives.Messages.Outgoing.Perk;
using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.MysteryBox.Snapshots;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Wallet;

namespace Vortex.PacketHandlers.Handshake;

public class SSOTicketMessageHandler(
    IAuthenticationService authService,
    ISessionGateway sessionGateway,
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    ICfhTicketService cfhTickets,
    IBuildersClubService buildersClubService,
    ILogger<SSOTicketMessageHandler> logger
) : IMessageHandler<SSOTicketMessage>
{
    private static readonly ImmutableArray<string> DefaultMessageTemplates =
    [
        "Please mind your language.",
        "This behaviour is not tolerated on this hotel.",
        "Please treat other users with respect.",
    ];

    private readonly IAuthenticationService _authService = authService;
    private readonly ISessionGateway _sessionGateway = sessionGateway;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IPermissionService _permissionService = permissionService;
    private readonly ICfhTicketService _cfhTickets = cfhTickets;
    private readonly IBuildersClubService _buildersClubService = buildersClubService;
    private readonly ILogger<SSOTicketMessageHandler> _logger = logger;

    public async ValueTask HandleAsync(
        SSOTicketMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        try
        {
            string ticket = message.SSO;
            int playerId = await _authService
                .GetPlayerIdFromTicketAsync(ticket, ctx.RemoteIpAddress, ct)
                .ConfigureAwait(false);

            if (playerId <= 0)
            {
                await ctx.CloseSessionAsync().ConfigureAwait(false);

                return;
            }

            DateTime? banExpiry = await _grainFactory
                .GetPlayerGrain(PlayerId.Parse(playerId))
                .GetActiveBanExpiryAsync(ct)
                .ConfigureAwait(false);

            if (banExpiry is not null)
            {
                string banMessage = SanctionDuration.IsPermanent(banExpiry.Value)
                    ? "You have been permanently banned from this hotel."
                    : $"You are banned until {banExpiry.Value:yyyy-MM-dd HH:mm} UTC.";

                await ctx.SendComposerAsync(
                        new UserBannedMessageComposer { Message = banMessage },
                        ct
                    )
                    .ConfigureAwait(false);
                await ctx.CloseSessionAsync().ConfigureAwait(false);

                return;
            }

            await _sessionGateway
                .AddSessionToPlayerAsync(ctx.SessionKey, playerId, ct)
                .ConfigureAwait(false);

            ClubSubscriptionSnapshot sub = await _grainFactory
                .GetPlayerGrain(PlayerId.Parse(playerId))
                .GetClubSubscriptionAsync(ct)
                .ConfigureAwait(false);

            ClubLevelType clubLevel = sub.IsActive
                ? (sub.IsVip ? ClubLevelType.Vip : ClubLevelType.Club)
                : ClubLevelType.None;

            PermissionSet permissions = await _permissionService
                .ResolveForPlayerAsync(playerId, ct)
                .ConfigureAwait(false);

            SecurityLevelType securityLevel = SecurityLevelPolicy.Resolve(permissions);

            // The client's new-user flow is driven entirely by these actions: 0 asks it to run the
            // look-and-name onboarding, 1 to pick a starter room. Both are read once, here — an
            // empty array means the player goes straight to the hotel view.
            // (WIN63 HabboLandingView.as::isOnboardingRequired(), and OnBoardingHcFlow's
            // AVATAR_NAME_CHANGE / NEW_ROOM_SELECT constants.)
            // Stamped before anything else that can fail: the mod tool's "minutes since last login"
            // is only meaningful if every successful handshake records one.
            await _grainFactory
                .GetPlayerGrain(PlayerId.Parse(playerId))
                .MarkLoggedInAsync(ct)
                .ConfigureAwait(false);

            bool nuxCompleted = await _grainFactory
                .GetPlayerGrain(PlayerId.Parse(playerId))
                .IsNuxCompletedAsync(ct)
                .ConfigureAwait(false);

            int currentHomeRoomId = await _grainFactory
                .GetPlayerNavigatorGrain(playerId)
                .GetHomeRoomIdAsync(ct)
                .ConfigureAwait(false);

            List<short> suggestedLoginActions = [];

            if (!nuxCompleted)
            {
                suggestedLoginActions.Add(SuggestedLoginAction.AvatarNameChange);
            }

            if (currentHomeRoomId <= 0)
            {
                suggestedLoginActions.Add(SuggestedLoginAction.NewRoomSelect);
            }

            await ctx.SendComposerAsync(
                    new AuthenticationOKMessage
                    {
                        AccountId = playerId,
                        SuggestedLoginActions = [.. suggestedLoginActions],
                        IdentityId = playerId,
                    },
                    ct
                )
                .ConfigureAwait(false);
            ImmutableArray<AvatarEffectSnapshot> effects = await _grainFactory
                .GetPlayerEffectGrain(playerId)
                .GetEffectsAsync(ct)
                .ConfigureAwait(false);
            await ctx.SendComposerAsync(new AvatarEffectsMessageComposer { Effects = effects }, ct)
                .ConfigureAwait(false);
            int homeRoomId = currentHomeRoomId;

            await ctx.SendComposerAsync(
                    new NavigatorSettingsMessageComposer
                    {
                        HomeRoomId = homeRoomId,
                        RoomIdToEnter = 0,
                    },
                    ct
                )
                .ConfigureAwait(false);
            await ctx.SendComposerAsync(
                    new FavouritesMessageComposer { Limit = 0, FavoriteRoomIds = [] },
                    ct
                )
                .ConfigureAwait(false);
            // unseen items
            // The clothing the account has unlocked. This used to be two empty arrays, which told
            // every player at every login that they own no wearable sets -- the avatar editor then
            // greys out everything they have ever redeemed, and a clothing furni they have already
            // bound asks to be redeemed a second time.
            PlayerClothingSnapshot clothing = await _grainFactory
                .GetPlayerClothingGrain(ctx.PlayerId)
                .GetUnlockedAsync(ct)
                .ConfigureAwait(false);

            await ctx.SendComposerAsync(
                    new FigureSetIdsEventMessageComposer
                    {
                        FigureSetIds = clothing.FigureSetIds,
                        BoundFurnitureNames = clothing.BoundFurnitureNames,
                    },
                    ct
                )
                .ConfigureAwait(false);
            await ctx.SendComposerAsync(
                    new NoobnessLevelMessage { NoobnessLevel = NoobnessLevelType.NotNoob },
                    ct
                )
                .ConfigureAwait(false);
            await ctx.SendComposerAsync(
                    new UserRightsMessage
                    {
                        ClubLevel = clubLevel,
                        SecurityLevel = securityLevel,
                        IsAmbassador = false,
                    },
                    ct
                )
                .ConfigureAwait(false);

            if (
                permissions.HasAny(
                    Capabilities.Moderation.Cfh,
                    Capabilities.Moderation.Chatlogs,
                    Capabilities.Moderation.Kick,
                    Capabilities.Moderation.Mute,
                    Capabilities.Moderation.Alert,
                    Capabilities.Moderation.Ban,
                    Capabilities.Room.ModerateAny
                )
            )
            {
                await SendModeratorBootstrapAsync(ctx, playerId, permissions, ct)
                    .ConfigureAwait(false);
            }

            // Vortex-specific: tells the client whether to offer the in-client furni editor's
            // button. Sent unconditionally, including the false case, so the client never has to
            // infer the answer from a missing packet. It is a UI hint only — both furni-editor
            // handlers re-check the capability on every request.
            await ctx.SendComposerAsync(
                    new VortexFurniEditorRightsMessageComposer
                    {
                        CanEdit = permissions.Has(Capabilities.Room.FurniEdit),
                    },
                    ct
                )
                .ConfigureAwait(false);

            if (sub.IsActive)
            {
                await ctx.SendComposerAsync(BuildScrSendUserInfo(sub), ct).ConfigureAwait(false);
            }

            if (sub.GiftsAvailable > 0)
            {
                await ctx.SendComposerAsync(
                        new ClubGiftNotificationEventMessageComposer
                        {
                            GiftsAvailable = sub.GiftsAvailable,
                        },
                        ct
                    )
                    .ConfigureAwait(false);
            }

            IPlayerWalletGrain wallet = _grainFactory.GetPlayerWalletGrain(playerId);
            int credits = await wallet
                .GetAmountForCurrencyAsync(
                    new CurrencyKind { CurrencyType = CurrencyType.Credits },
                    ct
                )
                .ConfigureAwait(false);
            Dictionary<int, int> activityPoints = await wallet
                .GetActivityPointsAsync(ct)
                .ConfigureAwait(false);

            await ctx.SendComposerAsync(
                    new CreditBalanceEventMessageComposer { Balance = $"{credits}.0" },
                    ct
                )
                .ConfigureAwait(false);
            await ctx.SendComposerAsync(
                    new ActivityPointsMessageComposer { PointsByCategoryId = activityPoints },
                    ct
                )
                .ConfigureAwait(false);

            // Silver and emeralds go out here with the credits rather than being left to the
            // client to ask for. It only asks once, while its inventory component is starting up,
            // and it keeps the answer in the catalogue purse for the whole session -- so a player
            // who opens the Collectors Guild before their inventory would read both as zero, with a
            // wallet that actually holds thousands.
            int silver = await wallet
                .GetAmountForCurrencyAsync(
                    new CurrencyKind { CurrencyType = CurrencyType.Silver },
                    ct
                )
                .ConfigureAwait(false);
            int emeralds = await wallet
                .GetAmountForCurrencyAsync(
                    new CurrencyKind { CurrencyType = CurrencyType.Emeralds },
                    ct
                )
                .ConfigureAwait(false);

            await ctx.SendComposerAsync(
                    new SilverBalanceMessageComposer { SilverBalance = silver },
                    ct
                )
                .ConfigureAwait(false);
            await ctx.SendComposerAsync(
                    new EmeraldBalanceMessageComposer { EmeraldBalance = emeralds },
                    ct
                )
                .ConfigureAwait(false);

            await ctx.SendComposerAsync(
                    new AvailabilityStatusMessageComposer
                    {
                        IsOpen = true,
                        OnShutDown = false,
                        IsAuthenticHabbo = true,
                    },
                    ct
                )
                .ConfigureAwait(false);
            await ctx.SendComposerAsync(new InfoFeedEnableMessageComposer { Enabled = true }, ct)
                .ConfigureAwait(false);
            await ctx.SendComposerAsync(new AchievementsScoreEventMessageComposer { Score = 0 }, ct)
                .ConfigureAwait(false);
            await ctx.SendComposerAsync(
                    new IsFirstLoginOfDayMessage { IsFirstLoginOfDay = true },
                    ct
                )
                .ConfigureAwait(false);
            // Drives the mystery box toolbar tracker. Sent inline rather than pushed by the grain so
            // it keeps its place in the login sequence; the grain owns every later refresh.
            MysteryBoxTrackerSnapshot mysteryBoxTracker = await _grainFactory
                .GetPlayerMysteryBoxGrain(playerId)
                .GetTrackerAsync(ct)
                .ConfigureAwait(false);

            await ctx.SendComposerAsync(
                    new MysteryBoxKeysMessageComposer
                    {
                        BoxColor = mysteryBoxTracker.BoxColor,
                        KeyColor = mysteryBoxTracker.KeyColor,
                    },
                    ct
                )
                .ConfigureAwait(false);
            BuildersClubSubscriptionSnapshot buildersClub = await _buildersClubService
                .GetSubscriptionAsync(playerId, ct)
                .ConfigureAwait(false);
            int buildersClubSecondsLeft = buildersClub.IsActive
                ? (int)Math.Max(0, (buildersClub.ExpiresAt!.Value - DateTime.UtcNow).TotalSeconds)
                : 0;

            await ctx.SendComposerAsync(
                    new BuildersClubSubscriptionStatusMessageComposer
                    {
                        SecondsLeft = buildersClubSecondsLeft,
                        FurniLimit = buildersClub.FurniLimit,
                        MaxFurniLimit = buildersClub.FurniLimit,
                        SecondsLeftWithGrace = buildersClubSecondsLeft,
                    },
                    ct
                )
                .ConfigureAwait(false);
            await ctx.SendComposerAsync(
                    new PerkAllowancesMessageComposer
                    {
                        Perks =
                        [
                            new PerkAllowanceItem
                            {
                                Code = "NAVIGATOR_ROOM_THUMBNAIL_CAMERA",
                                ErrorMessage = string.Empty,
                                IsAllowed = true,
                            },
                            new PerkAllowanceItem
                            {
                                Code = "JUDGE_CHAT_REVIEWS",
                                ErrorMessage = "requirement.unfulfilled.helper_level_6",
                                IsAllowed = false,
                            },
                            new PerkAllowanceItem
                            {
                                Code = "MOUSE_ZOOM",
                                ErrorMessage = string.Empty,
                                IsAllowed = true,
                            },
                            new PerkAllowanceItem
                            {
                                Code = "HABBO_CLUB_OFFER_BETA",
                                ErrorMessage = string.Empty,
                                IsAllowed = true,
                            },
                            new PerkAllowanceItem
                            {
                                Code = "TRADE",
                                ErrorMessage = "requirement.unfulfilled.citizenship_level_3",
                                IsAllowed = true,
                            },
                            new PerkAllowanceItem
                            {
                                Code = "CAMERA",
                                ErrorMessage = string.Empty,
                                IsAllowed = true,
                            },
                            new PerkAllowanceItem
                            {
                                Code = "NAVIGATOR_PHASE_TWO_2014",
                                ErrorMessage = string.Empty,
                                IsAllowed = true,
                            },
                            new PerkAllowanceItem
                            {
                                Code = "BUILDER_AT_WORK",
                                ErrorMessage = "requirement.unfulfilled.group_membership",
                                IsAllowed = false,
                            },
                            new PerkAllowanceItem
                            {
                                Code = "CALL_ON_HELPERS",
                                ErrorMessage = string.Empty,
                                IsAllowed = true,
                            },
                            new PerkAllowanceItem
                            {
                                Code = "CITIZEN",
                                ErrorMessage = string.Empty,
                                IsAllowed = true,
                            },
                            new PerkAllowanceItem
                            {
                                Code = "USE_GUIDE_TOOL",
                                ErrorMessage = "requirement.unfulfilled.helper_level_4",
                                IsAllowed = false,
                            },
                            new PerkAllowanceItem
                            {
                                Code = "VOTE_IN_COMPETITIONS",
                                ErrorMessage = "requirement.unfulfilled.helper_level_2",
                                IsAllowed = false,
                            },
                        ],
                    },
                    ct
                )
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            await CloseSessionSafelyAsync(ctx).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to complete SSO handshake for session {SessionKey}",
                ctx.SessionKey
            );

            await CloseSessionSafelyAsync(ctx).ConfigureAwait(false);
        }
    }

    /// <summary>Pushes the staff mod tool's bootstrap payload proactively at login, matching the
    /// real client: nothing in the WIN63 source ever requests ModeratorInit/CfhTopicsInit — the
    /// server just sends them to whoever has moderation rights.</summary>
    private async Task SendModeratorBootstrapAsync(
        MessageContext ctx,
        int playerId,
        PermissionSet permissions,
        CancellationToken ct
    )
    {
        ImmutableArray<CfhIssueQueueEntrySnapshot> issues = await _cfhTickets
            .GetOpenQueueAsync(ct)
            .ConfigureAwait(false);
        ImmutableArray<CfhCategorySnapshot> catalog = await _cfhTickets
            .GetCatalogAsync(ct)
            .ConfigureAwait(false);

        bool alertPermission = permissions.HasAny(Capabilities.Moderation.Alert);
        bool kickPermission = permissions.HasAny(Capabilities.Moderation.Kick);

        await ctx.SendComposerAsync(
                new ModeratorInitMessageComposer
                {
                    Issues = issues,
                    MessageTemplates = DefaultMessageTemplates,
                    CfhPermission = permissions.HasAny(Capabilities.Moderation.Cfh),
                    ChatlogsPermission = permissions.HasAny(Capabilities.Moderation.Chatlogs),
                    AlertPermission = alertPermission,
                    KickPermission = kickPermission,
                    BanPermission = permissions.HasAny(Capabilities.Moderation.Ban),
                    RoomAlertPermission = alertPermission,
                    RoomKickPermission = kickPermission,
                    RoomMessageTemplates = DefaultMessageTemplates,
                },
                ct
            )
            .ConfigureAwait(false);
        await ctx.SendComposerAsync(new CfhTopicsInitMessageComposer { Categories = catalog }, ct)
            .ConfigureAwait(false);

        // Restore where they left the window. Skipped when never positioned — a rectangle of zeroes
        // would collapse the tool to nothing on open.
        // Uses the ticket's player id, never ctx.PlayerId: the MessageContext for this packet was
        // built before the handler bound the session to a player, so ctx.PlayerId is still -1 here.
        PlayerModToolPreferencesSnapshot modToolPreferences = await _grainFactory
            .GetPlayerGrain(PlayerId.Parse(playerId))
            .GetModToolPreferencesAsync(ct)
            .ConfigureAwait(false);

        if (modToolPreferences.IsSet)
        {
            await ctx.SendComposerAsync(
                    new ModeratorToolPreferencesEventMessageComposer
                    {
                        WindowX = modToolPreferences.WindowX,
                        WindowY = modToolPreferences.WindowY,
                        WindowWidth = modToolPreferences.WindowWidth,
                        WindowHeight = modToolPreferences.WindowHeight,
                    },
                    ct
                )
                .ConfigureAwait(false);
        }
    }

    private async Task CloseSessionSafelyAsync(MessageContext ctx)
    {
        try
        {
            await ctx.CloseSessionAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(
                ex,
                "Failed to close session {SessionKey} after SSO handshake failure",
                ctx.SessionKey
            );
        }
    }

    private static ScrSendUserInfoMessageComposer BuildScrSendUserInfo(ClubSubscriptionSnapshot sub)
    {
        int daysLeft = sub.DaysLeft;
        int rem = daysLeft % 31;

        return new ScrSendUserInfoMessageComposer
        {
            ProductName = "habbo_club",
            DaysToPeriodEnd = rem == 0 ? 31 : rem,
            MemberPeriods = sub.TotalMonths,
            PeriodsSubscribedAhead = daysLeft / 31 - (rem == 0 ? 1 : 0),
            ResponseType = 2,
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
