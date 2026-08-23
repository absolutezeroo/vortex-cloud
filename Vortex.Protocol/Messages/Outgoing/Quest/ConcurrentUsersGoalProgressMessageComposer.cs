using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Quests;

namespace Vortex.Protocol.Messages.Outgoing.Quest;

/// <summary>
/// The landing-view "players online" goal: how many are on right now, the target, and where this
/// player stands with the reward.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record ConcurrentUsersGoalProgressMessageComposer : IComposer
{
    [Id(0)]
    public required ConcurrentUsersGoalState State { get; init; }

    /// <summary>Players online right now.</summary>
    [Id(1)]
    public required int UserCount { get; init; }

    /// <summary>How many are needed to unlock the reward.</summary>
    [Id(2)]
    public required int UserCountGoal { get; init; }
}
