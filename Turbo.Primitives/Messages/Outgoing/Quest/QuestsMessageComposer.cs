using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Quests.Snapshots;

namespace Turbo.Primitives.Messages.Outgoing.Quest;

/// <summary>The player's quest list, plus whether the client should open the quest window.</summary>
[GenerateSerializer, Immutable]
public sealed record QuestsMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<QuestSnapshot> Quests { get; init; }

    [Id(1)]
    public required bool OpenWindow { get; init; }
}
