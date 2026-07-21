using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Players;
using Vortex.Logging;
using Vortex.Players.Configuration;
using Vortex.Primitives;
using Vortex.Primitives.Events;
using Vortex.Primitives.Inventory.Grains;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Wallet;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Players.Grains;

internal sealed partial class PlayerGrain : Grain, IPlayerGrain
{
    private readonly ClubConfig _clubConfig;
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory;
    private readonly IEventPublisher _events;
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<PlayerGrain> _logger;

    private readonly PlayerLiveState _state;

    public PlayerGrain(
        IDbContextFactory<VortexDbContext> dbCtxFactory,
        IGrainFactory grainFactory,
        IEventPublisher events,
        ILogger<PlayerGrain> logger,
        IOptions<ClubConfig> clubConfig
    )
    {
        _dbCtxFactory = dbCtxFactory;
        _grainFactory = grainFactory;
        _events = events;
        _logger = logger;
        _clubConfig = clubConfig.Value;

        _state = new PlayerLiveState { PlayerId = PlayerId.Parse((int)this.GetPrimaryKeyLong()) };
    }

    public async Task SetFigureAsync(string figure, AvatarGenderType gender, CancellationToken ct)
    {
        _state.Figure = figure;
        _state.Gender = gender;

        await WriteToDatabaseAsync(ct);

        IPlayerPresenceGrain playerPresence = _grainFactory.GetPlayerPresenceGrain(
            (int)this.GetPrimaryKeyLong()
        );

        await playerPresence.OnFigureUpdatedAsync(await GetSummaryAsync(ct), ct);

        await WriteToDatabaseAsync(ct);

        await _events
            .PublishAsync(new PlayerFigureChangedEvent(_state.PlayerId, figure), ct)
            .ConfigureAwait(true);
    }

    public async Task<bool> TryGiveRespectAsync(int dailyLimit, CancellationToken ct)
    {
        (bool allowed, int givenToday, DateTime resetDate) = RespectBudget.TryConsume(
            _state.RespectGivenToday,
            _state.RespectResetDate,
            DateTime.Now,
            dailyLimit
        );

        _state.RespectGivenToday = givenToday;
        _state.RespectResetDate = resetDate;

        if (!allowed)
        {
            return false;
        }

        await WriteToDatabaseAsync(ct);

        return true;
    }

    public async Task<int> AddRespectReceivedAsync(CancellationToken ct)
    {
        _state.RespectReceived += 1;

        await WriteToDatabaseAsync(ct);

        return _state.RespectReceived;
    }

    public async Task<int> AddAchievementScoreAsync(int delta, CancellationToken ct)
    {
        if (delta == 0)
        {
            return _state.AchievementScore;
        }

        _state.AchievementScore += delta;

        await WriteToDatabaseAsync(ct);

        return _state.AchievementScore;
    }

    public async Task SetNameAsync(string name, CancellationToken ct)
    {
        _state.Name = name;

        await WriteToDatabaseAsync(ct);

        await _grainFactory
            .GetPlayerDirectoryGrain()
            .SetPlayerNameAsync(PlayerId.Parse((int)this.GetPrimaryKeyLong()), name, ct);

        IPlayerPresenceGrain playerPresence = _grainFactory.GetPlayerPresenceGrain(
            (int)this.GetPrimaryKeyLong()
        );

        await playerPresence.OnPlayerUpdatedAsync(await GetSummaryAsync(ct), ct);
    }

    public async Task SetMottoAsync(string text, CancellationToken ct)
    {
        _state.Motto = text;

        await WriteToDatabaseAsync(ct);

        IPlayerPresenceGrain playerPresence = _grainFactory.GetPlayerPresenceGrain(
            (int)this.GetPrimaryKeyLong()
        );

        await playerPresence.OnPlayerUpdatedAsync(await GetSummaryAsync(ct), ct);

        await WriteToDatabaseAsync(ct);

        await _events
            .PublishAsync(new PlayerMottoChangedEvent(_state.PlayerId, text), ct)
            .ConfigureAwait(true);
    }

    public async Task SetChatStylePreferenceAsync(int chatStyle, CancellationToken ct)
    {
        // No-op when unchanged so a repeated toggle from the client never touches the database.
        if (_state.RoomChatStyleId == chatStyle)
        {
            return;
        }

        _state.RoomChatStyleId = chatStyle;

        // Targeted single-column update: RoomChatStyleId is not part of the shared
        // WriteToDatabaseAsync property set, so writing it here can neither clobber nor be clobbered
        // by the other player fields.
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        await dbCtx
            .Players.Where(p => p.Id == (int)_state.PlayerId)
            .ExecuteUpdateAsync(up => up.SetProperty(p => p.RoomChatStyleId, chatStyle), ct)
            .ConfigureAwait(false);
    }

    public Task<int> GetChatStylePreferenceAsync(CancellationToken ct) =>
        Task.FromResult(_state.RoomChatStyleId);

    public Task<PlayerSummarySnapshot> GetSummaryAsync(CancellationToken ct)
    {
        return Task.FromResult(
            new PlayerSummarySnapshot
            {
                PlayerId = _state.PlayerId,
                Name = _state.Name,
                Motto = _state.Motto,
                Figure = _state.Figure,
                Gender = _state.Gender,
                AchievementScore = _state.AchievementScore,
                CreatedAt = _state.CreatedAt,
            }
        );
    }

    public Task<PlayerExtendedProfileSnapshot> GetExtendedProfileSnapshotAsync(CancellationToken ct)
    {
        return Task.FromResult(
            new PlayerExtendedProfileSnapshot
            {
                UserId = _state.PlayerId,
                UserName = _state.Name,
                Figure = _state.Figure,
                Motto = _state.Motto,
                CreationDate = _state.CreatedAt.ToString("yyyy-MM-dd"),
                AchievementScore = _state.AchievementScore,
                FriendCount = 0,
                IsFriend = false,
                IsFriendRequestSent = false,
                IsOnline = true,
                Guilds = [],
                LastAccessSinceInSeconds = 0,
                OpenProfileWindow = true,
                IsHidden = false,
                AccountLevel = 1,
                IntegerField24 = 0,
                StarGemCount = 0,
                BooleanField26 = false,
                BooleanField27 = false,
            }
        );
    }

    public Task<ClubSubscriptionSnapshot> GetClubSubscriptionAsync(CancellationToken ct)
    {
        bool isActive = _state.ClubLevel > 0 && _state.ClubExpiresAt > DateTime.UtcNow;
        int daysLeft = isActive ? (int)(_state.ClubExpiresAt - DateTime.UtcNow).TotalDays : 0;

        return Task.FromResult(
            new ClubSubscriptionSnapshot
            {
                IsActive = isActive,
                IsVip = isActive && _state.ClubLevel >= 2,
                ExpiresAt = _state.ClubExpiresAt,
                DaysLeft = daysLeft,
                TotalMonths = _state.ClubTotalMonths,
                GiftsAvailable = _state.ClubGiftsAvailable,
                NextGiftAt = _state.ClubNextGiftAt,
                PaydayAt = _state.KickbackPaydayAt,
                CreditsSpentThisPeriod = _state.KickbackCreditsSpent,
                TotalCreditsRewarded = _state.KickbackTotalRewarded,
                TotalCreditsSpent = _state.KickbackTotalSpent,
                PastClubDays = _state.ClubPastClubDays,
                PastVipDays = _state.ClubPastVipDays,
            }
        );
    }

    public async Task<ClubPurchaseResult> PurchaseClubAsync(
        int months,
        bool isVip,
        int costCredits,
        CancellationToken ct
    )
    {
        if (months <= 0)
        {
            return ClubPurchaseResult.Success;
        }

        IPlayerWalletGrain walletGrain = _grainFactory.GetPlayerWalletGrain(_state.PlayerId);

        WalletDebitResult debitResult = await walletGrain
            .TryDebitAsync(
                [
                    new WalletDebitRequest
                    {
                        CurrencyKind = new CurrencyKind { CurrencyType = CurrencyType.Credits },
                        Amount = costCredits,
                    },
                ],
                ct
            )
            .ConfigureAwait(true);

        if (!debitResult.Succeeded)
        {
            return ClubPurchaseResult.NotEnoughCredits;
        }

        DateTime now = DateTime.UtcNow;
        bool isRenewal = _state.ClubLevel > 0 && _state.ClubExpiresAt > now;

        // Streak (consecutive membership months) keeps running while active, and survives a short
        // lapse (grace window). A longer gap resets the streak so it starts over from this purchase.
        int newTotalMonths;
        if (isRenewal)
        {
            newTotalMonths = _state.ClubTotalMonths + months;
        }
        else
        {
            bool hadPriorSub = _state.ClubExpiresAt > DateTime.MinValue;
            double lapsedDays = hadPriorSub
                ? (now - _state.ClubExpiresAt).TotalDays
                : double.MaxValue;
            newTotalMonths =
                lapsedDays > _clubConfig.StreakGraceDays ? months : _state.ClubTotalMonths + months;
        }

        DateTime baseDate = isRenewal ? _state.ClubExpiresAt : now;
        DateTime newExpiry = baseDate.AddMonths(months);
        int newLevel = isVip ? 2 : 1;
        int newGiftsAvailable = _state.ClubGiftsAvailable + months;

        // First purchase or expired sub: gift cycle starts now + 31 days.
        // Renewal: keep existing cycle so the player doesn't lose their countdown.
        DateTime? newNextGiftAt =
            isRenewal && _state.ClubNextGiftAt.HasValue
                ? _state.ClubNextGiftAt
                : now.AddDays(_clubConfig.GiftCycleDays);

        // Lifetime membership days (never reset, VIP tracked separately) feed the club info window.
        _state.ClubPastClubDays += months * _clubConfig.GiftCycleDays;
        if (isVip)
        {
            _state.ClubPastVipDays += months * _clubConfig.GiftCycleDays;
        }

        _state.ClubLevel = newLevel;
        _state.ClubExpiresAt = newExpiry;
        _state.ClubTotalMonths = newTotalMonths;
        _state.ClubGiftsAvailable = newGiftsAvailable;
        _state.ClubNextGiftAt = newNextGiftAt;
        _state.ClubLastExpiredAt = null;
        _state.ClubFirstSubscribedAt ??= now;

        // Grant the membership badge(s) once active. Idempotent at the inventory layer.
        await GrantClubBadgesAsync(isVip, ct).ConfigureAwait(true);

        // Init payday on first subscription; renewals keep the existing cycle
        if (_state.KickbackPaydayAt is null)
        {
            _state.KickbackPaydayAt = now.AddDays(_clubConfig.GiftCycleDays);
        }

        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        PlayerSubscriptionEntity? existing = await dbCtx.PlayerSubscriptions.FirstOrDefaultAsync(
            x =>
                x.PlayerEntityId == (int)_state.PlayerId
                && x.SubscriptionType == SubscriptionType.HabboClub,
            ct
        );

        if (existing is not null)
        {
            existing.Level = newLevel;
            existing.ExpiresAt = newExpiry;
            existing.TotalMonths = newTotalMonths;
            existing.GiftsAvailable = newGiftsAvailable;
            existing.NextGiftAt = newNextGiftAt;
            existing.PastClubDays = _state.ClubPastClubDays;
            existing.PastVipDays = _state.ClubPastVipDays;
            existing.FirstSubscribedAt = _state.ClubFirstSubscribedAt;
            existing.LastExpiredAt = null;
            existing.HcBadgeGranted = _state.ClubBadgeGranted;
        }
        else
        {
            PlayerEntity playerEntity =
                await dbCtx.Players.FindAsync([_state.PlayerId.Value], ct)
                ?? throw new VortexException(VortexErrorCodeEnum.PlayerNotFound);

            dbCtx.PlayerSubscriptions.Add(
                new PlayerSubscriptionEntity
                {
                    PlayerEntityId = _state.PlayerId,
                    SubscriptionType = SubscriptionType.HabboClub,
                    Level = newLevel,
                    ExpiresAt = newExpiry,
                    TotalMonths = newTotalMonths,
                    GiftsAvailable = newGiftsAvailable,
                    NextGiftAt = newNextGiftAt,
                    PastClubDays = _state.ClubPastClubDays,
                    PastVipDays = _state.ClubPastVipDays,
                    FirstSubscribedAt = _state.ClubFirstSubscribedAt,
                    HcBadgeGranted = _state.ClubBadgeGranted,
                    PlayerEntity = playerEntity,
                }
            );
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        await UpsertKickbackAsync(ct).ConfigureAwait(true);

        await _events
            .PublishAsync(
                new ClubPurchasedEvent(
                    _state.PlayerId,
                    months,
                    isVip,
                    isRenewal,
                    costCredits,
                    newTotalMonths
                ),
                ct
            )
            .ConfigureAwait(true);

        return ClubPurchaseResult.Success;
    }

    public async Task<bool> TryConsumeClubGiftAsync(string productCode, CancellationToken ct)
    {
        if (_state.ClubGiftsAvailable <= 0)
        {
            return false;
        }

        _state.ClubGiftsAvailable--;

        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        await dbCtx
            .PlayerSubscriptions.Where(x =>
                x.PlayerEntityId == (int)_state.PlayerId
                && x.SubscriptionType == SubscriptionType.HabboClub
            )
            .ExecuteUpdateAsync(
                up => up.SetProperty(p => p.GiftsAvailable, _state.ClubGiftsAvailable),
                ct
            )
            .ConfigureAwait(true);

        await _events
            .PublishAsync(new ClubGiftClaimedEvent(_state.PlayerId, productCode), ct)
            .ConfigureAwait(true);

        return true;
    }

    public async Task TrackCreditSpendAsync(int credits, CancellationToken ct)
    {
        if (credits <= 0 || _state.ClubLevel == 0)
        {
            return;
        }

        _state.KickbackCreditsSpent += credits;
        _state.KickbackTotalSpent += credits;

        await UpsertKickbackAsync(ct).ConfigureAwait(true);
    }

    public async Task<bool> ApplyAccountBanAsync(
        int actorPlayerId,
        DateTime? bannedUntil,
        string reason,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        int? accountId = await FindLinkedAccountIdAsync(dbCtx, ct).ConfigureAwait(true);

        if (accountId is null)
        {
            return false;
        }

        // IgnoreQueryFilters: an unban soft-deletes the row, so a later re-ban must find and revive
        // that soft-deleted row rather than insert a duplicate under the unique account index.
        AccountBanEntity? existing = await dbCtx
            .AccountBans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.PlayerAccountEntityId == accountId, ct)
            .ConfigureAwait(true);

        if (bannedUntil is null)
        {
            if (existing is not null)
            {
                existing.DeletedAt = DateTime.UtcNow;
                await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
            }
        }
        else if (existing is null)
        {
            dbCtx.AccountBans.Add(
                new AccountBanEntity
                {
                    PlayerAccountEntityId = accountId.Value,
                    DateExpires = bannedUntil.Value,
                    Reason = reason,
                    PlayerAccountEntity = null!,
                }
            );
            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
        }
        else
        {
            existing.DateExpires = bannedUntil.Value;
            existing.Reason = reason;
            existing.DeletedAt = null;
            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
        }

        await _events
            .PublishAsync(
                new PlayerAccountBannedEvent(
                    actorPlayerId,
                    (int)_state.PlayerId,
                    bannedUntil,
                    reason
                ),
                ct
            )
            .ConfigureAwait(true);

        return true;
    }

    public async Task<bool> ApplyTradingLockAsync(
        int actorPlayerId,
        DateTime? lockedUntil,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        PlayerEntity? player = await dbCtx
            .Players.FindAsync([_state.PlayerId.Value], ct)
            .ConfigureAwait(true);

        if (player is null)
        {
            return false;
        }

        player.TradingLockedUntil = lockedUntil;

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        await _events
            .PublishAsync(
                new PlayerTradingLockedEvent(actorPlayerId, (int)_state.PlayerId, lockedUntil),
                ct
            )
            .ConfigureAwait(true);

        return true;
    }

    public async Task<DateTime?> GetActiveBanExpiryAsync(CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        int? accountId = await FindLinkedAccountIdAsync(dbCtx, ct).ConfigureAwait(true);

        if (accountId is null)
        {
            return null;
        }

        DateTime now = DateTime.UtcNow;

        AccountBanEntity? activeBan = await dbCtx
            .AccountBans.AsNoTracking()
            .FirstOrDefaultAsync(
                b =>
                    b.PlayerAccountEntityId == accountId
                    && b.DeletedAt == null
                    && b.DateExpires > now,
                ct
            )
            .ConfigureAwait(true);

        return activeBan?.DateExpires;
    }

    private async Task<int?> FindLinkedAccountIdAsync(VortexDbContext dbCtx, CancellationToken ct)
    {
        PlayerEntity? player = await dbCtx
            .Players.FindAsync([_state.PlayerId.Value], ct)
            .ConfigureAwait(true);

        return player?.PlayerAccountEntityId;
    }

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await HydrateAsync(ct);

        TimeSpan maintenanceInterval = TimeSpan.FromMilliseconds(_clubConfig.MaintenanceIntervalMs);

        this.RegisterGrainTimer<object?>(
            async (_, ct) => await RunMaintenanceAsync(ct),
            null,
            maintenanceInterval,
            maintenanceInterval
        );
    }

    /// <summary>
    ///     Hourly club maintenance: detect expiry, grant due gift tokens, and pay out kickback.
    ///     Each step is isolated so one failing step never blocks the others, and failures are logged
    ///     rather than silently swallowed by the timer runtime.
    /// </summary>
    private async Task RunMaintenanceAsync(CancellationToken ct)
    {
        try
        {
            await CheckExpirationAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Club expiry check failed for player {PlayerId}",
                (int)_state.PlayerId
            );
        }

        try
        {
            await CheckAndGrantGiftTokensAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Club gift grant failed for player {PlayerId}",
                (int)_state.PlayerId
            );
        }

        try
        {
            await CheckAndGrantPaydayAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Club payday failed for player {PlayerId}", (int)_state.PlayerId);
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct)
    {
        await WriteToDatabaseAsync(ct);
        await UpsertKickbackAsync(ct);
    }

    private async Task CheckAndGrantGiftTokensAsync(CancellationToken ct)
    {
        if (_state.ClubLevel == 0 || _state.ClubExpiresAt <= DateTime.UtcNow)
        {
            return;
        }

        if (_state.ClubNextGiftAt is null || _state.ClubNextGiftAt > DateTime.UtcNow)
        {
            return;
        }

        int tokensToGrant = 0;
        DateTime nextGift = _state.ClubNextGiftAt.Value;
        DateTime now = DateTime.UtcNow;

        while (nextGift <= now)
        {
            tokensToGrant++;
            nextGift = nextGift.AddDays(_clubConfig.GiftCycleDays);
        }

        _state.ClubGiftsAvailable += tokensToGrant;
        _state.ClubNextGiftAt = nextGift;

        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        await dbCtx
            .PlayerSubscriptions.Where(x =>
                x.PlayerEntityId == (int)_state.PlayerId
                && x.SubscriptionType == SubscriptionType.HabboClub
            )
            .ExecuteUpdateAsync(
                up =>
                    up.SetProperty(p => p.GiftsAvailable, _state.ClubGiftsAvailable)
                        .SetProperty(p => p.NextGiftAt, _state.ClubNextGiftAt),
                ct
            )
            .ConfigureAwait(true);

        await _events
            .PublishAsync(
                new ClubGiftTokenGrantedEvent(
                    _state.PlayerId,
                    tokensToGrant,
                    _state.ClubGiftsAvailable
                ),
                ct
            )
            .ConfigureAwait(true);
    }

    private async Task HydrateAsync(CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        PlayerEntity entity =
            await dbCtx
                .Players.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == (int)_state.PlayerId, ct)
            ?? throw new VortexException(VortexErrorCodeEnum.PlayerNotFound);

        _state.Name = entity.Name;
        _state.Motto = entity.Motto ?? string.Empty;
        _state.Figure = entity.Figure;
        _state.Gender = entity.Gender;
        _state.RoomChatStyleId = entity.RoomChatStyleId ?? 0;
        _state.AchievementScore = entity.AchievementScore;
        _state.RespectReceived = entity.RespectReceived;
        _state.RespectGivenToday = entity.RespectGivenToday;
        _state.RespectResetDate = entity.RespectResetDate;
        _state.CreatedAt = entity.CreatedAt;
        _state.LastUpdated = entity.UpdatedAt;

        PlayerSubscriptionEntity? sub = await dbCtx
            .PlayerSubscriptions.AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.PlayerEntityId == (int)_state.PlayerId
                    && x.SubscriptionType == SubscriptionType.HabboClub,
                ct
            );

        if (sub is not null)
        {
            // Lifetime / streak bookkeeping is loaded regardless of active state.
            _state.ClubTotalMonths = sub.TotalMonths;
            _state.ClubPastClubDays = sub.PastClubDays;
            _state.ClubPastVipDays = sub.PastVipDays;
            _state.ClubFirstSubscribedAt = sub.FirstSubscribedAt;
            _state.ClubLastExpiredAt = sub.LastExpiredAt;
            _state.ClubBadgeGranted = sub.HcBadgeGranted;
            _state.ClubExpiresAt = sub.ExpiresAt;

            if (sub.ExpiresAt > DateTime.UtcNow)
            {
                _state.ClubLevel = sub.Level;
                _state.ClubGiftsAvailable = sub.GiftsAvailable;
                _state.ClubNextGiftAt = sub.NextGiftAt;

                await CheckAndGrantGiftTokensAsync(ct);
            }
            else if (sub.Level > 0)
            {
                // Membership lapsed while offline — surface the expiry once.
                _state.ClubLevel = sub.Level;
                await CheckExpirationAsync(ct);
            }
        }

        PlayerKickbackEntity? kickback = await dbCtx
            .PlayerKickbacks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PlayerEntityId == (int)_state.PlayerId, ct);

        if (kickback is not null)
        {
            _state.KickbackPaydayAt = kickback.PaydayAt;
            _state.KickbackCreditsSpent = kickback.CreditsSpent;
            _state.KickbackTotalRewarded = kickback.TotalRewarded;
            _state.KickbackTotalSpent = kickback.TotalSpent;

            await CheckAndGrantPaydayAsync(ct);
        }

        await _grainFactory
            .GetPlayerDirectoryGrain()
            .SetPlayerNameAsync(PlayerId.Parse((int)this.GetPrimaryKeyLong()), _state.Name, ct);
    }

    private async Task WriteToDatabaseAsync(CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        PlayerSummarySnapshot snapshot = await GetSummaryAsync(ct);

        await dbCtx
            .Players.Where(x => x.Id == (int)_state.PlayerId)
            .ExecuteUpdateAsync(
                up =>
                    up.SetProperty(p => p.Name, snapshot.Name)
                        .SetProperty(p => p.Motto, snapshot.Motto)
                        .SetProperty(p => p.Figure, snapshot.Figure)
                        .SetProperty(p => p.Gender, snapshot.Gender)
                        .SetProperty(p => p.AchievementScore, snapshot.AchievementScore)
                        .SetProperty(p => p.RespectReceived, _state.RespectReceived)
                        .SetProperty(p => p.RespectGivenToday, _state.RespectGivenToday)
                        .SetProperty(p => p.RespectResetDate, _state.RespectResetDate),
                ct
            );

        _state.LastUpdated = DateTime.Now;
    }

    private async Task GrantClubBadgesAsync(bool isVip, CancellationToken ct)
    {
        try
        {
            IInventoryGrain inventory = _grainFactory.GetInventoryGrain(
                (int)this.GetPrimaryKeyLong()
            );

            if (!_state.ClubBadgeGranted)
            {
                await inventory.GrantBadgeAsync(_clubConfig.ClubBadgeCode, ct).ConfigureAwait(true);
                _state.ClubBadgeGranted = true;
            }

            if (isVip)
            {
                await inventory.GrantBadgeAsync(_clubConfig.VipBadgeCode, ct).ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to grant club badge for player {PlayerId}",
                (int)_state.PlayerId
            );
        }
    }

    private async Task CheckExpirationAsync(CancellationToken ct)
    {
        if (_state.ClubLevel == 0)
        {
            return;
        }

        if (_state.ClubExpiresAt > DateTime.UtcNow)
        {
            return;
        }

        // Already surfaced this exact expiry — avoid firing the event twice.
        if (_state.ClubLastExpiredAt == _state.ClubExpiresAt)
        {
            return;
        }

        bool wasVip = _state.ClubLevel >= 2;
        _state.ClubLastExpiredAt = _state.ClubExpiresAt;
        _state.ClubLevel = 0;

        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        await dbCtx
            .PlayerSubscriptions.Where(x =>
                x.PlayerEntityId == (int)_state.PlayerId
                && x.SubscriptionType == SubscriptionType.HabboClub
            )
            .ExecuteUpdateAsync(
                up => up.SetProperty(p => p.LastExpiredAt, _state.ClubLastExpiredAt),
                ct
            )
            .ConfigureAwait(true);

        await _events
            .PublishAsync(new ClubExpiredEvent(_state.PlayerId, wasVip), ct)
            .ConfigureAwait(true);

        _logger.LogInformation(
            "Club membership expired for player {PlayerId} (wasVip={WasVip})",
            (int)_state.PlayerId,
            wasVip
        );
    }

    private async Task CheckAndGrantPaydayAsync(CancellationToken ct)
    {
        if (_state.ClubLevel == 0 || _state.ClubExpiresAt <= DateTime.UtcNow)
        {
            return;
        }

        if (_state.KickbackPaydayAt is null || _state.KickbackPaydayAt > DateTime.UtcNow)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;
        DateTime payday = _state.KickbackPaydayAt.Value;

        while (payday <= now)
        {
            int monthlyReward = (int)(
                _state.KickbackCreditsSpent * (_clubConfig.KickbackPercent / 100.0)
            );
            int streakBonus = Math.Min(_state.ClubTotalMonths, _clubConfig.GiftCycleDays);
            int totalReward = monthlyReward + streakBonus;

            if (totalReward > 0)
            {
                IPlayerWalletGrain wallet = _grainFactory.GetPlayerWalletGrain(
                    (int)this.GetPrimaryKeyLong()
                );
                await wallet.GrantCreditsAsync(totalReward, ct).ConfigureAwait(true);
                _state.KickbackTotalRewarded += totalReward;

                await _events
                    .PublishAsync(new ClubPaydayEvent(_state.PlayerId, totalReward), ct)
                    .ConfigureAwait(true);
            }

            _state.KickbackCreditsSpent = 0;
            payday = payday.AddDays(_clubConfig.GiftCycleDays);
        }

        _state.KickbackPaydayAt = payday;

        await UpsertKickbackAsync(ct).ConfigureAwait(true);
    }

    private async Task UpsertKickbackAsync(CancellationToken ct)
    {
        if (_state.KickbackPaydayAt is null)
        {
            return;
        }

        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        PlayerKickbackEntity? existing = await dbCtx.PlayerKickbacks.FirstOrDefaultAsync(
            x => x.PlayerEntityId == (int)_state.PlayerId,
            ct
        );

        if (existing is not null)
        {
            existing.PaydayAt = _state.KickbackPaydayAt;
            existing.CreditsSpent = _state.KickbackCreditsSpent;
            existing.TotalRewarded = _state.KickbackTotalRewarded;
            existing.TotalSpent = _state.KickbackTotalSpent;
        }
        else
        {
            PlayerEntity playerEntity =
                await dbCtx.Players.FindAsync([(int)_state.PlayerId], ct)
                ?? throw new VortexException(VortexErrorCodeEnum.PlayerNotFound);

            dbCtx.PlayerKickbacks.Add(
                new PlayerKickbackEntity
                {
                    PlayerEntityId = _state.PlayerId,
                    PaydayAt = _state.KickbackPaydayAt,
                    CreditsSpent = _state.KickbackCreditsSpent,
                    TotalRewarded = _state.KickbackTotalRewarded,
                    TotalSpent = _state.KickbackTotalSpent,
                    PlayerEntity = playerEntity,
                }
            );
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
    }
}
