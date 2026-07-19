using Orleans;

namespace Turbo.Primitives.Quests.Snapshots;

/// <summary>A cached quest definition (one row of the <c>quests</c> table).</summary>
[GenerateSerializer, Immutable]
public sealed record QuestDefinitionSnapshot
{
    [Id(0)]
    public required int Id { get; init; }

    [Id(1)]
    public required string CampaignCode { get; init; }

    [Id(2)]
    public required string ChainCode { get; init; }

    [Id(3)]
    public required string LocalizationCode { get; init; }

    [Id(4)]
    public required string QuestType { get; init; }

    [Id(5)]
    public required int TotalSteps { get; init; }

    [Id(6)]
    public required int RewardType { get; init; }

    [Id(7)]
    public required int RewardAmount { get; init; }

    [Id(8)]
    public required string CatalogPageName { get; init; }

    [Id(9)]
    public required string ImageVersion { get; init; }

    [Id(10)]
    public required int SortOrder { get; init; }

    [Id(11)]
    public required bool Easy { get; init; }

    [Id(12)]
    public required bool Seasonal { get; init; }

    [Id(13)]
    public required int SeasonalSeconds { get; init; }

    [Id(14)]
    public System.DateTime? EndsAt { get; init; }
}
