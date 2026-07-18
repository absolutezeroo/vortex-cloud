using Orleans;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Players.Snapshots;

namespace Turbo.Primitives.Messages.Outgoing.Inventory.Achievements;

/// <summary>A single achievement's updated standing, pushed after progression.</summary>
[GenerateSerializer, Immutable]
public sealed record AchievementEventMessageComposer : IComposer
{
    [Id(0)]
    public required AchievementProgressSnapshot Achievement { get; init; }
}
