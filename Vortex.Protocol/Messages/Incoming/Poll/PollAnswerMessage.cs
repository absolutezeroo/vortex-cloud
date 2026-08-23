using System.Collections.Immutable;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Poll;

/// <summary>
/// One answered question. The client sends this for every question it walks through — including the
/// live word-quiz, which reuses the same message rather than having one of its own.
/// </summary>
public record PollAnswerMessage : IMessageEvent
{
    public required int PollId { get; init; }

    public required int QuestionId { get; init; }

    /// <summary>
    /// The picked choice values, or a single entry holding the typed text. A checkbox question can
    /// legitimately send several; an unanswered one sends none.
    /// </summary>
    public required ImmutableArray<string> Answers { get; init; }
}
