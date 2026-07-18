using Orleans;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Quests.Snapshots;

namespace Turbo.Primitives.Messages.Outgoing.Quest;

/// <summary>A quest was completed. <see cref="ShowDialog"/> tells the client whether to pop the
/// completion dialog.</summary>
[GenerateSerializer, Immutable]
public sealed record QuestCompletedMessageComposer : IComposer
{
    [Id(0)]
    public required QuestSnapshot Quest { get; init; }

    [Id(1)]
    public required bool ShowDialog { get; init; }
}
