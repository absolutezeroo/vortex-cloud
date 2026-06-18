using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Players.Configuration;
using Turbo.Primitives.Messages.Incoming.Users;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Users;

public class ScrGetKickbackInfoMessageHandler(
    IGrainFactory grainFactory,
    IOptions<ClubConfig> clubConfig
) : IMessageHandler<ScrGetKickbackInfoMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ClubConfig _clubConfig = clubConfig.Value;

    public async ValueTask HandleAsync(
        ScrGetKickbackInfoMessage message,
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

        var now = DateTime.UtcNow;
        var streakBonus = Math.Min(sub.TotalMonths, 31);
        var monthlyReward = (int)(
            sub.CreditsSpentThisPeriod * (_clubConfig.KickbackPercent / 100.0)
        );
        var minutesUntilPayday =
            sub.IsActive && sub.PaydayAt.HasValue && sub.PaydayAt.Value > now
                ? (int)(sub.PaydayAt.Value - now).TotalMinutes
                : 0;

        await ctx.SendComposerAsync(
                new ScrSendKickbackInfoMessageComposer
                {
                    CurrentHcStreak = sub.TotalMonths,
                    FirstSubscriptionDate =
                        sub.IsActive && sub.TotalMonths > 0
                            ? sub.ExpiresAt.AddMonths(-sub.TotalMonths).ToString("yyyy-MM-dd")
                            : string.Empty,
                    KickbackPercentage = sub.IsActive ? _clubConfig.KickbackPercent / 100.0 : 0.0,
                    TotalCreditsMissed = 0,
                    TotalCreditsRewarded = sub.TotalCreditsRewarded,
                    TotalCreditsSpent = sub.TotalCreditsSpent,
                    CreditRewardForStreakBonus = sub.IsActive ? streakBonus : 0,
                    CreditRewardForMonthlySpent = sub.IsActive ? monthlyReward : 0,
                    TimeUntilPayday = minutesUntilPayday,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
