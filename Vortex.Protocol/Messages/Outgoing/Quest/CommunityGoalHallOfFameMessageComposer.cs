using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Quest;

/// <summary>
/// The leaderboard for a community goal (header 363).
///
/// Shape from WIN63's parser
/// (com/sulake/habbo/communication/messages/parser/quest/_SafeCls_4188.as), which delegates to the
/// DTO in unknowns/_SafePkg_1976/_SafeCls_4488.as: a string, a count, then that many entries.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record CommunityGoalHallOfFameMessageComposer : IComposer
{
    [Id(0)]
    public required string GoalCode { get; init; }

    [Id(1)]
    public required ImmutableArray<CommunityGoalHallOfFameEntry> Entries { get; init; }
}

/// <summary>
/// One place on the leaderboard. Fields from unknowns/_SafePkg_1976/_SafeCls_4504.as.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record CommunityGoalHallOfFameEntry
{
    [Id(0)]
    public required int UserId { get; init; }

    [Id(1)]
    public required string UserName { get; init; }

    [Id(2)]
    public required string Figure { get; init; }

    [Id(3)]
    public required int Rank { get; init; }

    [Id(4)]
    public required int CurrentScore { get; init; }
}
