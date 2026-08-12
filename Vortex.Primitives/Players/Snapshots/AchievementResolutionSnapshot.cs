using Orleans;

namespace Vortex.Primitives.Players.Snapshots;

/// <summary>
/// One achievement offered by a resolution statue, as the picker dialog draws it.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record AchievementResolutionSnapshot
{
    [Id(0)]
    public required int AchievementId { get; init; }

    /// <summary>The level the player has already cleared — what the dialog labels "your current
    /// level". Completed levels, not the level they are working toward.</summary>
    [Id(1)]
    public required int Level { get; init; }

    /// <summary>Badge of the level they would have to reach; the dialog draws it greyed out while
    /// the row is not selectable, and reads its name and description from the badge itself.</summary>
    [Id(2)]
    public required string BadgeId { get; init; }

    [Id(3)]
    public required int RequiredLevel { get; init; }

    [Id(4)]
    public required AchievementResolutionState State { get; init; }
}
