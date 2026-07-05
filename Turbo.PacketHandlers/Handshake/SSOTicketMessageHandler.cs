using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Authentication;
using Turbo.Primitives.Messages.Incoming.Handshake;
using Turbo.Primitives.Messages.Outgoing.Availability;
using Turbo.Primitives.Messages.Outgoing.Catalog;
using Turbo.Primitives.Messages.Outgoing.Handshake;
using Turbo.Primitives.Messages.Outgoing.Inventory.Achievements;
using Turbo.Primitives.Messages.Outgoing.Inventory.Avatareffect;
using Turbo.Primitives.Messages.Outgoing.Inventory.Clothing;
using Turbo.Primitives.Messages.Outgoing.Inventory.Purse;
using Turbo.Primitives.Messages.Outgoing.Moderation;
using Turbo.Primitives.Messages.Outgoing.Mysterybox;
using Turbo.Primitives.Messages.Outgoing.Navigator;
using Turbo.Primitives.Messages.Outgoing.Notifications;
using Turbo.Primitives.Messages.Outgoing.Perk;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Players;
using Turbo.Primitives.Permissions;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Enums;
using Turbo.Primitives.Players.Enums.Wallet;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Players.Wallet;

namespace Turbo.PacketHandlers.Handshake;

public class SSOTicketMessageHandler(
    IAuthenticationService authService,
    ISessionGateway sessionGateway,
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    ILogger<SSOTicketMessageHandler> logger
) : IMessageHandler<SSOTicketMessage>
{
    private readonly IAuthenticationService _authService = authService;
    private readonly ISessionGateway _sessionGateway = sessionGateway;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IPermissionService _permissionService = permissionService;
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

            await ctx.SendComposerAsync(
                    new AuthenticationOKMessage
                    {
                        AccountId = playerId,
                        SuggestedLoginActions = [],
                        IdentityId = playerId,
                    },
                    ct
                )
                .ConfigureAwait(false);
            await ctx.SendComposerAsync(new AvatarEffectsMessageComposer { Effects = [] }, ct)
                .ConfigureAwait(false);
            await ctx.SendComposerAsync(
                    new NavigatorSettingsMessageComposer { HomeRoomId = 0, RoomIdToEnter = 0 },
                    ct
                )
                .ConfigureAwait(false);
            await ctx.SendComposerAsync(
                    new FavouritesMessageComposer { Limit = 0, FavoriteRoomIds = [] },
                    ct
                )
                .ConfigureAwait(false);
            // unseen items
            await ctx.SendComposerAsync(
                    new FigureSetIdsEventMessageComposer
                    {
                        FigureSetIds = [],
                        BoundFurnitureNames = [],
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
            await ctx.SendComposerAsync(
                    new MysteryBoxKeysMessageComposer
                    {
                        BoxColor = string.Empty,
                        KeyColor = string.Empty,
                    },
                    ct
                )
                .ConfigureAwait(false);
            await ctx.SendComposerAsync(
                    new BuildersClubSubscriptionStatusMessageComposer
                    {
                        SecondsLeft = 0,
                        FurniLimit = 0,
                        MaxFurniLimit = 0,
                        SecondsLeftWithGrace = 0,
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
