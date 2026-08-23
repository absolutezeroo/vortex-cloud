using System.Collections.Immutable;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Help;

/// <summary>
/// The finished quiz. <see cref="Answers"/> is positional: one entry per question, in the order the
/// server sent the ids, holding the index of the option the player chose. The client shuffles how
/// the options are displayed but names each one by its true index, so what arrives here is the real
/// answer and not a screen position.
/// </summary>
public record PostQuizAnswersMessage : IMessageEvent
{
    public required string QuizCode { get; init; }

    public required ImmutableArray<int> Answers { get; init; }
}
