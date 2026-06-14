using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record ScrSendKickbackInfoMessageComposer : IComposer
{
    [Id(0)]
    public int CurrentHcStreak { get; init; }

    [Id(1)]
    public string FirstSubscriptionDate { get; init; } = string.Empty;

    [Id(2)]
    public double KickbackPercentage { get; init; }

    [Id(3)]
    public int TotalCreditsMissed { get; init; }

    [Id(4)]
    public int TotalCreditsRewarded { get; init; }

    [Id(5)]
    public int TotalCreditsSpent { get; init; }

    [Id(6)]
    public int CreditRewardForStreakBonus { get; init; }

    [Id(7)]
    public int CreditRewardForMonthlySpent { get; init; }

    [Id(8)]
    public int TimeUntilPayday { get; init; }
}
