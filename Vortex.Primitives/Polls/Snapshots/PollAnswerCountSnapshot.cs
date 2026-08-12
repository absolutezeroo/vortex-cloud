using Orleans;

namespace Vortex.Primitives.Polls.Snapshots;

/// <summary>How many players in the room have picked one answer of a live word-quiz question.</summary>
[GenerateSerializer, Immutable]
public sealed record PollAnswerCountSnapshot
{
    /// <summary>The answer value being counted.</summary>
    [Id(0)]
    public required string Answer { get; init; }

    [Id(1)]
    public required int Count { get; init; }
}
