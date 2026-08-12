using System.Collections.Generic;
using System.Collections.Immutable;

namespace Vortex.Players.Help;

/// <summary>
/// Marks a submitted quiz. Pure, and worth being pure: the answers arrive positionally against the
/// question list the server itself sent, so grading is an index-alignment problem, and an
/// index-alignment bug marks the wrong questions wrong without anything failing.
/// </summary>
public static class QuizGrader
{
    /// <summary>
    /// The question numbers the player got wrong, in the order they were asked. Empty means a pass,
    /// which is exactly what the client reads it as.
    /// </summary>
    /// <param name="questions">(question number, correct answer index), in the order sent.</param>
    /// <param name="answers">Chosen answer index per question, positionally.</param>
    public static ImmutableArray<int> Grade(
        IReadOnlyList<(int QuestionNumber, int CorrectAnswerIndex)> questions,
        IReadOnlyList<int> answers
    )
    {
        ImmutableArray<int>.Builder wrong = ImmutableArray.CreateBuilder<int>();

        for (int i = 0; i < questions.Count; i++)
        {
            // A short submission is not a partial pass. The client only submits once every question
            // is answered, so a missing entry means the packet was hand-made — and the safe reading
            // of "no answer" is "not the right one", never "close enough".
            if (i >= answers.Count || answers[i] != questions[i].CorrectAnswerIndex)
            {
                wrong.Add(questions[i].QuestionNumber);
            }
        }

        return wrong.ToImmutable();
    }
}
