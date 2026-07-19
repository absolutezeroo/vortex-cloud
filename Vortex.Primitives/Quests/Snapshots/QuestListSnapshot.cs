using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Quests.Snapshots;

/// <summary>A player's quest list plus whether the client should open the quest window.</summary>
[GenerateSerializer, Immutable]
public sealed record QuestListSnapshot
{
    [Id(0)]
    public required ImmutableArray<QuestSnapshot> Quests { get; init; }

    [Id(1)]
    public required bool OpenWindow { get; init; }
}
