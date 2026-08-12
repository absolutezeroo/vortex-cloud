using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Help;

/// <summary>
/// Opening a quiz. The client names which one — <c>HabboWay1</c> from the Habbo Way booklet,
/// <c>SafetyQuiz1</c> from the safety one — so this is not a "give me the quiz" ping.
/// </summary>
public record GetQuizQuestionsMessage : IMessageEvent
{
    public required string QuizCode { get; init; }
}
