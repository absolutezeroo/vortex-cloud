using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Players.Snapshots;

namespace Turbo.Primitives.Messages.Outgoing.Inventory.Achievements;

/// <summary>The player's full achievements list plus the tab the client opens by default.</summary>
[GenerateSerializer, Immutable]
public sealed record AchievementsEventMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<AchievementProgressSnapshot> Achievements { get; init; }

    [Id(1)]
    public required string DefaultCategory { get; init; }
}
