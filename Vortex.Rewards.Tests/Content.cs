using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Vortex.Primitives.Habbicons;
using Vortex.Primitives.Habbicons.Snapshots;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.RewardTracks.Snapshots;

namespace Vortex.Rewards.Tests;

/// <summary>
/// Builders for the content the tests run against.
/// </summary>
/// <remarks>
/// Every default here is the boring case, so a test names only what it is about: a track with one
/// counter task and one milestone says so in one line, and the reader's attention goes to the
/// assertion rather than to twelve required properties.
/// </remarks>
internal static class Content
{
    public static RewardTrackTaskLevelSnapshot Level(
        int index,
        int required,
        int points,
        bool premium = false
    ) =>
        new()
        {
            LevelIndex = index,
            RequiredCount = required,
            PointsReward = points,
            Premium = premium,
        };

    public static RewardTrackTaskDefinitionSnapshot Task(
        string taskId = "task",
        string actionCode = "act",
        string parameter = "",
        TaskProgressMode mode = TaskProgressMode.Counter,
        bool premium = false,
        params RewardTrackTaskLevelSnapshot[] levels
    ) =>
        new()
        {
            TaskId = taskId,
            ActionCode = actionCode,
            Parameter = parameter,
            Mode = mode,
            Premium = premium,
            SortOrder = 0,
            Levels = levels.Length == 0 ? [Level(0, 1, 10)] : [.. levels],
        };

    /// <summary>A task carrying extra conditions, which are ANDed with each other and the parameter.</summary>
    public static RewardTrackTaskDefinitionSnapshot TaskWith(
        TaskProgressMode mode = TaskProgressMode.Counter,
        string parameter = "",
        params RewardTrackTaskConditionSnapshot[] conditions
    ) => Task(mode: mode, parameter: parameter) with { Conditions = [.. conditions] };

    public static RewardTrackTaskConditionSnapshot Condition(
        TaskConditionField field,
        TaskConditionOperator op,
        string value
    ) =>
        new()
        {
            Field = field,
            Operator = op,
            Value = value,
        };

    public static RewardGrantSnapshot Reward(
        RewardKind kind = RewardKind.Currency,
        string typeId = "0",
        int amount = 1
    ) =>
        new()
        {
            Kind = kind,
            RewardTypeId = typeId,
            Amount = amount,
            ExtraParams = string.Empty,
        };

    public static RewardTrackPrizeDefinitionSnapshot Prize(
        string prizeId,
        int requiredPoints,
        bool premium = false,
        params RewardGrantSnapshot[] rewards
    ) =>
        new()
        {
            PrizeId = prizeId,
            RequiredPoints = requiredPoints,
            Premium = premium,
            SortOrder = 0,
            Rewards = rewards.Length == 0 ? [Reward()] : [.. rewards],
        };

    public static RewardTrackPremiumSnapshot Premium(
        int boostPerMille = 1200,
        int instantPoints = 0,
        int costCredits = 0,
        int costDiamonds = 25
    ) =>
        new()
        {
            BoostPerMille = boostPerMille,
            InstantPoints = instantPoints,
            CostCredits = costCredits,
            CostDiamonds = costDiamonds,
        };

    public static RewardTrackDefinitionSnapshot Track(
        string trackId = "track",
        RewardTrackStatus status = RewardTrackStatus.Active,
        RewardTrackPremiumSnapshot? premium = null,
        RewardTrackCompletionPolicy completion = RewardTrackCompletionPolicy.AllFreePrizesClaimed,
        RewardTrackUnlockKind unlockKind = RewardTrackUnlockKind.Always,
        string unlockValue = "",
        DateTime? startsAt = null,
        DateTime? progressEndsAt = null,
        DateTime? claimEndsAt = null,
        IEnumerable<RewardTrackTaskDefinitionSnapshot>? tasks = null,
        IEnumerable<RewardTrackPrizeDefinitionSnapshot>? prizes = null
    ) =>
        new()
        {
            TrackId = trackId,
            Theme = "blue",
            Status = status,
            SortOrder = 0,
            StartsAtUtc = startsAt,
            ProgressEndsAtUtc = progressEndsAt,
            ClaimEndsAtUtc = claimEndsAt,
            UnlockKind = unlockKind,
            UnlockValue = unlockValue,
            CompletionPolicy = completion,
            Premium = premium,
            Tasks = tasks is null ? [] : [.. tasks],
            Prizes = prizes is null ? [] : [.. prizes],
            ContentVersion = 1,
            Hidden = false,
        };

    public static PlayerRewardTrackStateSnapshot State(
        string trackId = "track",
        int points = 0,
        bool premiumUnlocked = false,
        IEnumerable<PlayerTaskProgressSnapshot>? tasks = null,
        IEnumerable<string>? claimed = null
    ) =>
        new()
        {
            TrackId = trackId,
            Points = points,
            PremiumUnlocked = premiumUnlocked,
            Tasks = tasks is null ? [] : [.. tasks],
            ClaimedPrizeIds = claimed is null ? [] : [.. claimed],
            ContentVersion = 1,
        };

    public static PlayerTaskProgressSnapshot Progress(
        string taskId,
        int count,
        int highestPaid = -1
    ) =>
        new()
        {
            TaskId = taskId,
            ProgressCount = count,
            HighestPaidLevelIndex = highestPaid,
        };

    public static UnlockFactsBuilder Facts() => new();

    public static HabbiconDefinitionSnapshot Habbicon(
        int id,
        int collectionId = 1,
        bool isReward = false,
        int priceCredits = 0
    ) =>
        new()
        {
            HabbiconId = id,
            Code = $"code_{id}",
            CollectionId = collectionId,
            SortOrder = id,
            IsCollectionReward = isReward,
            PriceCredits = priceCredits,
            PriceActivityPoints = 0,
            ActivityPointType = 0,
            Enabled = true,
        };

    public static HabbiconCollectionSnapshot Collection(
        int collectionId = 1,
        int entryCount = 3,
        bool withReward = true
    ) =>
        new()
        {
            CollectionId = collectionId,
            Code = $"set_{collectionId}",
            SortOrder = 0,
            Enabled = true,
            Hidden = false,
            PriceCredits = 0,
            PriceActivityPoints = 0,
            ActivityPointType = 0,
            Entries =
            [
                .. Enumerable
                    .Range(1, entryCount)
                    .Select(i => Habbicon(collectionId * 100 + i, collectionId)),
            ],
            RewardHabbicon = withReward
                ? Habbicon(collectionId * 100 + 99, collectionId, isReward: true)
                : null,
        };

    public static Dictionary<int, HabbiconState> Owned(params int[] ids)
    {
        Dictionary<int, HabbiconState> owned = [];

        foreach (int id in ids)
        {
            owned[id] = HabbiconState.Owned;
        }

        return owned;
    }
}

/// <summary>A readable way to say what the player has satisfied, for the unlock rules.</summary>
internal sealed class UnlockFactsBuilder
{
    private readonly HashSet<string> _completed = new(StringComparer.Ordinal);
    private readonly HashSet<string> _claims = new(StringComparer.Ordinal);
    private readonly HashSet<string> _badges = new(StringComparer.Ordinal);
    private readonly Dictionary<string, bool> _flags = new(StringComparer.Ordinal);
    private int _ageDays;

    public UnlockFactsBuilder Completed(string trackId)
    {
        _completed.Add(trackId);

        return this;
    }

    public UnlockFactsBuilder Claimed(string trackId, string prizeId)
    {
        _claims.Add($"{trackId}:{prizeId}");

        return this;
    }

    public UnlockFactsBuilder Badge(string code)
    {
        _badges.Add(code);

        return this;
    }

    public UnlockFactsBuilder AgeDays(int days)
    {
        _ageDays = days;

        return this;
    }

    public UnlockFactsBuilder Flag(string key, bool on)
    {
        _flags[key] = on;

        return this;
    }

    public Vortex.RewardTracks.Progression.UnlockFacts Build() =>
        new(_completed, _claims, _badges, _ageDays, _flags);
}
