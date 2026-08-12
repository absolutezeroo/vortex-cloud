using Orleans;

namespace Vortex.Primitives.Polls.Snapshots;

/// <summary>One selectable answer of a poll question.</summary>
[GenerateSerializer, Immutable]
public sealed record PollChoiceSnapshot
{
    /// <summary>What the client sends back when this choice is picked.</summary>
    [Id(0)]
    public required string Value { get; init; }

    /// <summary>The label the player reads.</summary>
    [Id(1)]
    public required string ChoiceText { get; init; }

    /// <summary>NPS branch key; 0 = picking this choice leads to no follow-up question.</summary>
    [Id(2)]
    public required int ChoiceType { get; init; }
}
