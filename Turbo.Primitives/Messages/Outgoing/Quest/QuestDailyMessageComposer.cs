using Orleans;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Quests.Snapshots;

namespace Turbo.Primitives.Messages.Outgoing.Quest;

/// <summary>
/// The daily quest response. <see cref="Quest"/> is null when no daily quest is active; the
/// easy/hard counts describe the daily pool.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record QuestDailyMessageComposer : IComposer
{
    [Id(0)]
    public QuestSnapshot? Quest { get; init; }

    [Id(1)]
    public required int EasyQuestCount { get; init; }

    [Id(2)]
    public required int HardQuestCount { get; init; }
}
