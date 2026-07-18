using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Quests.Snapshots;

namespace Turbo.Primitives.Messages.Outgoing.Quest;

/// <summary>The player's seasonal quests.</summary>
[GenerateSerializer, Immutable]
public sealed record SeasonalQuestsMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<QuestSnapshot> Quests { get; init; }
}
