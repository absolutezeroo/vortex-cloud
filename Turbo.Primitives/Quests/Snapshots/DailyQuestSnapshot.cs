using Orleans;

namespace Turbo.Primitives.Quests.Snapshots;

/// <summary>
/// The daily quest response. <see cref="Quest"/> is null when no daily quest is available, in which
/// case the client is told there is none.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record DailyQuestSnapshot
{
    [Id(0)]
    public QuestSnapshot? Quest { get; init; }

    [Id(1)]
    public int EasyQuestCount { get; init; }

    [Id(2)]
    public int HardQuestCount { get; init; }
}
