using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Quests.Snapshots;

namespace Vortex.Primitives.Messages.Outgoing.Quest;

/// <summary>The player's seasonal quests.</summary>
[GenerateSerializer, Immutable]
public sealed record SeasonalQuestsMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<QuestSnapshot> Quests { get; init; }
}
