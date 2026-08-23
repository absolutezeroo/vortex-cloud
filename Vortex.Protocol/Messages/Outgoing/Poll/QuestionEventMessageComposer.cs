using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Polls.Snapshots;

namespace Vortex.Primitives.Messages.Outgoing.Poll;

/// <summary>
/// Puts one timed question in front of everyone in the room (the client's "word quiz"). Unlike a
/// survey this is room-wide and live: answers stream back as <see cref="QuestionAnsweredEventMessageComposer"/>
/// until <see cref="Duration"/> runs out.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record QuestionEventMessageComposer : IComposer
{
    /// <summary>Operator-defined tag carried through to the widget.</summary>
    [Id(0)]
    public required string PollType { get; init; }

    [Id(1)]
    public required int PollId { get; init; }

    [Id(2)]
    public required int QuestionId { get; init; }

    /// <summary>Seconds the widget stays open.</summary>
    [Id(3)]
    public required int Duration { get; init; }

    /// <summary>The question itself. Follow-ups are not read by the quiz widget.</summary>
    [Id(4)]
    public required PollQuestionSnapshot Question { get; init; }
}
