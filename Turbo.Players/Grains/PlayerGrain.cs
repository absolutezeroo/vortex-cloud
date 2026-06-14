using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Orleans;
using Turbo.Database.Context;
using Turbo.Database.Entities.Players;
using Turbo.Logging;
using Turbo.Primitives;
using Turbo.Primitives.Grains.Players;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Players;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Enums;
using Turbo.Primitives.Players.Enums.Wallet;
using Turbo.Primitives.Players.Wallet;
using Turbo.Primitives.Rooms.Enums;

namespace Turbo.Players.Grains;

internal sealed class PlayerGrain : Grain, IPlayerGrain
{
    private const int GiftCycleDays = 31;

    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory;
    private readonly IGrainFactory _grainFactory;

    private readonly PlayerLiveState _state;

    public PlayerGrain(IDbContextFactory<TurboDbContext> dbCtxFactory, IGrainFactory grainFactory)
    {
        _dbCtxFactory = dbCtxFactory;
        _grainFactory = grainFactory;

        _state = new() { PlayerId = PlayerId.Parse((int)this.GetPrimaryKeyLong()) };
    }

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await HydrateAsync(ct);

        this.RegisterGrainTimer<object?>(
            async (_, ct) => await CheckAndGrantGiftTokensAsync(ct),
            null,
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(1)
        );

        this.RegisterGrainTimer<object?>(
            async (_, ct) => await CheckAndGrantPaydayAsync(ct),
            null,
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(1)
        );
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct)
    {
        await WriteToDatabaseAsync(ct);
        await UpsertKickbackAsync(ct);
    }

    public async Task SetFigureAsync(string figure, AvatarGenderType gender, CancellationToken ct)
    {
        _state.Figure = figure;
        _state.Gender = gender;

        await WriteToDatabaseAsync(ct);

        var playerPresence = _grainFactory.GetPlayerPresenceGrain((int)this.GetPrimaryKeyLong());

        await playerPresence.OnFigureUpdatedAsync(await GetSummaryAsync(ct), ct);

        await WriteToDatabaseAsync(ct);
    }

    public async Task SetMottoAsync(string text, CancellationToken ct)
    {
        _state.Motto = text;

        await WriteToDatabaseAsync(ct);

        var playerPresence = _grainFactory.GetPlayerPresenceGrain((int)this.GetPrimaryKeyLong());

        await playerPresence.OnPlayerUpdatedAsync(await GetSummaryAsync(ct), ct);

        await WriteToDatabaseAsync(ct);
    }

    private async Task CheckAndGrantGiftTokensAsync(CancellationToken ct)
    {
        if (_state.ClubLevel == 0 || _state.ClubExpiresAt <= DateTime.UtcNow)
            return;

        if (_state.ClubNextGiftAt is null || _state.ClubNextGiftAt > DateTime.UtcNow)
            return;

        var tokensToGrant = 0;
        var nextGift = _state.ClubNextGiftAt.Value;
        var now = DateTime.UtcNow;

        while (nextGift <= now)
        {
            tokensToGrant++;
            nextGift = nextGift.AddDays(GiftCycleDays);
        }

        _state.ClubGiftsAvailable += tokensToGrant;
        _state.ClubNextGiftAt = nextGift;

        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        await dbCtx.PlayerSubscriptions
            .Where(x =>
                x.PlayerEntityId == (int)_state.PlayerId
                && x.SubscriptionType == SubscriptionType.HabboClub)
            .ExecuteUpdateAsync(
                up => up
                    .SetProperty(p => p.GiftsAvailable, _state.ClubGiftsAvailable)
                    .SetProperty(p => p.NextGiftAt, _state.ClubNextGiftAt),
                ct)
            .ConfigureAwait(false);
    }

    private async Task HydrateAsync(CancellationToken ct)
    {
        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        var entity =
            await dbCtx
                .Players.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == (int)_state.PlayerId, ct)
            ?? throw new TurboException(TurboErrorCodeEnum.PlayerNotFound);

        _state.Name = entity.Name;
        _state.Motto = entity.Motto ?? string.Empty;
        _state.Figure = entity.Figure;
        _state.Gender = entity.Gender;
        _state.AchievementScore = 0;
        _state.CreatedAt = entity.CreatedAt;
        _state.LastUpdated = entity.UpdatedAt;

        var sub = await dbCtx.PlayerSubscriptions.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.PlayerEntityId == (int)_state.PlayerId
                && x.SubscriptionType == SubscriptionType.HabboClub, ct);

        if (sub is not null && sub.ExpiresAt > DateTime.UtcNow)
        {
            _state.ClubLevel = sub.Level;
            _state.ClubExpiresAt = sub.ExpiresAt;
            _state.ClubTotalMonths = sub.TotalMonths;
            _state.ClubGiftsAvailable = sub.GiftsAvailable;
            _state.ClubNextGiftAt = sub.NextGiftAt;

            await CheckAndGrantGiftTokensAsync(ct);
        }

        var kickback = await dbCtx.PlayerKickbacks.AsNoTracking()
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
        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        var snapshot = await GetSummaryAsync(ct);

        await dbCtx
            .Players.Where(x => x.Id == (int)_state.PlayerId)
            .ExecuteUpdateAsync(
                up =>
                    up.SetProperty(p => p.Name, snapshot.Name)
                        .SetProperty(p => p.Motto, snapshot.Motto)
                        .SetProperty(p => p.Figure, snapshot.Figure)
                        .SetProperty(p => p.Gender, snapshot.Gender),
                ct
            );

        _state.LastUpdated = DateTime.Now;
    }

    public Task<PlayerSummarySnapshot> GetSummaryAsync(CancellationToken ct) =>
        Task.FromResult(
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
        var isActive = _state.ClubLevel > 0 && _state.ClubExpiresAt > DateTime.UtcNow;
        var daysLeft = isActive ? (int)(_state.ClubExpiresAt - DateTime.UtcNow).TotalDays : 0;

        return Task.FromResult(new ClubSubscriptionSnapshot
        {
            IsActive = isActive,
            IsVip = _state.ClubLevel >= 2,
            ExpiresAt = _state.ClubExpiresAt,
            DaysLeft = daysLeft,
            TotalMonths = _state.ClubTotalMonths,
            GiftsAvailable = _state.ClubGiftsAvailable,
            NextGiftAt = _state.ClubNextGiftAt,
            PaydayAt = _state.KickbackPaydayAt,
            CreditsSpentThisPeriod = _state.KickbackCreditsSpent,
            TotalCreditsRewarded = _state.KickbackTotalRewarded,
            TotalCreditsSpent = _state.KickbackTotalSpent,
        });
    }

    public async Task PurchaseClubAsync(int months, bool isVip, int costCredits, CancellationToken ct)
    {
        var walletGrain = _grainFactory.GetPlayerWalletGrain(_state.PlayerId);

        var debitResult = await walletGrain.TryDebitAsync(
            [
                new WalletDebitRequest
                {
                    CurrencyKind = new CurrencyKind { CurrencyType = CurrencyType.Credits },
                    Amount = costCredits,
                },
            ],
            ct
        ).ConfigureAwait(false);

        if (!debitResult.Succeeded)
            return;

        var now = DateTime.UtcNow;
        var isRenewal = _state.ClubLevel > 0 && _state.ClubExpiresAt > now;
        var baseDate = isRenewal ? _state.ClubExpiresAt : now;
        var newExpiry = baseDate.AddMonths(months);
        var newLevel = isVip ? 2 : 1;
        var newTotalMonths = _state.ClubTotalMonths + months;
        var newGiftsAvailable = _state.ClubGiftsAvailable + months;

        // First purchase or expired sub: gift cycle starts now + 31 days.
        // Renewal: keep existing cycle so the player doesn't lose their countdown.
        var newNextGiftAt = isRenewal && _state.ClubNextGiftAt.HasValue
            ? _state.ClubNextGiftAt
            : now.AddDays(GiftCycleDays);

        _state.ClubLevel = newLevel;
        _state.ClubExpiresAt = newExpiry;
        _state.ClubTotalMonths = newTotalMonths;
        _state.ClubGiftsAvailable = newGiftsAvailable;
        _state.ClubNextGiftAt = newNextGiftAt;

        // Init payday on first subscription; renewals keep the existing cycle
        if (_state.KickbackPaydayAt is null)
            _state.KickbackPaydayAt = now.AddDays(GiftCycleDays);

        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        var existing = await dbCtx.PlayerSubscriptions
            .FirstOrDefaultAsync(x =>
                x.PlayerEntityId == (int)_state.PlayerId
                && x.SubscriptionType == SubscriptionType.HabboClub, ct);

        if (existing is not null)
        {
            existing.Level = newLevel;
            existing.ExpiresAt = newExpiry;
            existing.TotalMonths = newTotalMonths;
            existing.GiftsAvailable = newGiftsAvailable;
            existing.NextGiftAt = newNextGiftAt;
        }
        else
        {
            var playerEntity = await dbCtx.Players
                .FindAsync([_state.PlayerId.Value], ct)
                ?? throw new TurboException(TurboErrorCodeEnum.PlayerNotFound);

            dbCtx.PlayerSubscriptions.Add(new PlayerSubscriptionEntity
            {
                PlayerEntityId = (int)_state.PlayerId,
                SubscriptionType = SubscriptionType.HabboClub,
                Level = newLevel,
                ExpiresAt = newExpiry,
                TotalMonths = newTotalMonths,
                GiftsAvailable = newGiftsAvailable,
                NextGiftAt = newNextGiftAt,
                PlayerEntity = playerEntity,
            });
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        await UpsertKickbackAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> TryConsumeClubGiftAsync(CancellationToken ct)
    {
        if (_state.ClubGiftsAvailable <= 0)
            return false;

        _state.ClubGiftsAvailable--;

        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        await dbCtx.PlayerSubscriptions
            .Where(x => x.PlayerEntityId == (int)_state.PlayerId && x.SubscriptionType == SubscriptionType.HabboClub)
            .ExecuteUpdateAsync(up => up.SetProperty(p => p.GiftsAvailable, _state.ClubGiftsAvailable), ct)
            .ConfigureAwait(false);

        return true;
    }

    public async Task TrackCreditSpendAsync(int credits, CancellationToken ct)
    {
        if (credits <= 0 || _state.ClubLevel == 0)
            return;

        _state.KickbackCreditsSpent += credits;
        _state.KickbackTotalSpent += credits;

        await UpsertKickbackAsync(ct).ConfigureAwait(false);
    }

    private async Task CheckAndGrantPaydayAsync(CancellationToken ct)
    {
        if (_state.ClubLevel == 0 || _state.ClubExpiresAt <= DateTime.UtcNow)
            return;

        if (_state.KickbackPaydayAt is null || _state.KickbackPaydayAt > DateTime.UtcNow)
            return;

        var now = DateTime.UtcNow;
        var payday = _state.KickbackPaydayAt.Value;

        while (payday <= now)
        {
            var monthlyReward = (int)(_state.KickbackCreditsSpent * 0.1);
            var streakBonus = Math.Min(_state.ClubTotalMonths, 31);
            var totalReward = monthlyReward + streakBonus;

            if (totalReward > 0)
            {
                var wallet = _grainFactory.GetPlayerWalletGrain((int)this.GetPrimaryKeyLong());
                await wallet.GrantCreditsAsync(totalReward, ct).ConfigureAwait(false);
                _state.KickbackTotalRewarded += totalReward;
            }

            _state.KickbackCreditsSpent = 0;
            payday = payday.AddDays(GiftCycleDays);
        }

        _state.KickbackPaydayAt = payday;

        await UpsertKickbackAsync(ct).ConfigureAwait(false);
    }

    private async Task UpsertKickbackAsync(CancellationToken ct)
    {
        if (_state.KickbackPaydayAt is null)
            return;

        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        var existing = await dbCtx.PlayerKickbacks
            .FirstOrDefaultAsync(x => x.PlayerEntityId == (int)_state.PlayerId, ct);

        if (existing is not null)
        {
            existing.PaydayAt = _state.KickbackPaydayAt;
            existing.CreditsSpent = _state.KickbackCreditsSpent;
            existing.TotalRewarded = _state.KickbackTotalRewarded;
            existing.TotalSpent = _state.KickbackTotalSpent;
        }
        else
        {
            var playerEntity = await dbCtx.Players
                .FindAsync([(int)_state.PlayerId], ct)
                ?? throw new TurboException(TurboErrorCodeEnum.PlayerNotFound);

            dbCtx.PlayerKickbacks.Add(new PlayerKickbackEntity
            {
                PlayerEntityId = (int)_state.PlayerId,
                PaydayAt = _state.KickbackPaydayAt,
                CreditsSpent = _state.KickbackCreditsSpent,
                TotalRewarded = _state.KickbackTotalRewarded,
                TotalSpent = _state.KickbackTotalSpent,
                PlayerEntity = playerEntity,
            });
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
