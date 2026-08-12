using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Quests.Snapshots;

/// <summary>One assigned daily task, wire-ready.</summary>
[GenerateSerializer, Immutable]
public sealed record DailyTaskSnapshot
{
    /// <summary>
    /// The assignment's id, not the definition's. It goes out as a long because the client reads it
    /// with readLong and keeps it as a Number.
    /// </summary>
    [Id(0)]
    public required long TaskId { get; init; }

    /// <summary>Localization key stem the client renders the name/description/hint from.</summary>
    [Id(1)]
    public required string TaskCode { get; init; }

    /// <summary>The objective type that advances it — the same vocabulary quests use.</summary>
    [Id(2)]
    public required string QuestTypeCode { get; init; }

    /// <summary>Bonus tasks sort last and raise their own notification when they appear.</summary>
    [Id(3)]
    public required bool IsBonus { get; init; }

    [Id(4)]
    public required string ImageVersion { get; init; }

    /// <summary>Catalog page the task's button links to; empty for none.</summary>
    [Id(5)]
    public required string CatalogName { get; init; }

    [Id(6)]
    public required int RequiredRepeats { get; init; }

    [Id(7)]
    public required int Repeats { get; init; }

    [Id(8)]
    public required DailyTaskStatus Status { get; init; }

    /// <summary>
    /// Seconds until the assignment lapses. Negative means expired — the client reads it that way
    /// rather than through a separate flag, so a lapsed task must go out with a negative number.
    /// </summary>
    [Id(9)]
    public required int SecondsLeft { get; init; }

    [Id(10)]
    public ImmutableArray<DailyTaskRewardSnapshot> Rewards { get; init; } =
        ImmutableArray<DailyTaskRewardSnapshot>.Empty;
}

/// <summary>What completing a daily task hands over.</summary>
[GenerateSerializer, Immutable]
public sealed record DailyTaskRewardSnapshot
{
    /// <summary>Product item type; written as a short, which is what the client reads.</summary>
    [Id(0)]
    public required short ProductItemTypeId { get; init; }

    /// <summary>Reward kind the client localizes (e.g. a currency or item key).</summary>
    [Id(1)]
    public required string RewardTypeId { get; init; }

    /// <summary>Free-form parameters passed through to the client.</summary>
    [Id(2)]
    public required string ExtraParams { get; init; }

    [Id(3)]
    public required int Amount { get; init; }
}
