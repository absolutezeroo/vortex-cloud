using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Quests.Snapshots;

namespace Vortex.Primitives.Messages.Outgoing.Quest;

/// <summary>A single quest (the quest tracker's active quest).</summary>
[GenerateSerializer, Immutable]
public sealed record QuestMessageComposer : IComposer
{
    [Id(0)]
    public required QuestSnapshot Quest { get; init; }
}
