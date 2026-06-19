using Orleans;

namespace Turbo.Primitives.Groups.Snapshots;

/// <summary>A group id paired with its badge code, for the client badge cache (HabboGroupBadges).</summary>
[GenerateSerializer, Immutable]
public sealed record GroupBadgeSnapshot
{
    [Id(0)]
    public required int GroupId { get; init; }

    [Id(1)]
    public required string BadgeCode { get; init; }
}
