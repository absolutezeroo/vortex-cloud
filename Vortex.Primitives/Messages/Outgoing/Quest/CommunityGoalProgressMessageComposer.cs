using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Quest;

/// <summary>
/// Where the hotel and this player stand on the active community goal (header 283).
///
/// Field order is the client's own DTO (unknowns/_SafePkg_1976/_SafeCls_4497.as), read by the parser
/// at com/sulake/habbo/communication/messages/parser/quest/_SafeCls_4122.as.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record CommunityGoalProgressMessageComposer : IComposer
{
    /// <summary>True once the goal's window has closed; the widget then shows the final state.</summary>
    [Id(0)]
    public required bool HasGoalExpired { get; init; }

    [Id(1)]
    public required int PersonalContributionScore { get; init; }

    /// <summary>This player's place among contributors; 0 when they have not contributed.</summary>
    [Id(2)]
    public required int PersonalContributionRank { get; init; }

    [Id(3)]
    public required int CommunityTotalScore { get; init; }

    /// <summary>Highest rung reached, 0 before the first threshold.</summary>
    [Id(4)]
    public required int CommunityHighestAchievedLevel { get; init; }

    /// <summary>How much more the hotel needs for the next rung; 0 on the last one.</summary>
    [Id(5)]
    public required int ScoreRemainingUntilNextLevel { get; init; }

    /// <summary>0–100 progress towards the next rung.</summary>
    [Id(6)]
    public required int PercentCompletionTowardsNextLevel { get; init; }

    [Id(7)]
    public required string GoalCode { get; init; }

    /// <summary>Seconds left in the goal's window; 0 when it has no deadline or has expired.</summary>
    [Id(8)]
    public required int TimeRemainingInSeconds { get; init; }

    /// <summary>How many contributors are rewarded at each rung, in level order.</summary>
    [Id(9)]
    public required ImmutableArray<int> RewardUserLimits { get; init; }
}
