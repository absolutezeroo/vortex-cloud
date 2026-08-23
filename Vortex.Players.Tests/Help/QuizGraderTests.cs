using System.Collections.Generic;
using FluentAssertions;
using Vortex.Social.Grains;
using Vortex.Social.Help;
using Xunit;

namespace Vortex.Players.Tests.Help;

/// <summary>
///     Marking the help quizzes. The answers arrive positionally against the question list the
///     server sent, and the question numbers are the client's localization keys — so what comes back
///     out has to be the numbers, never the positions.
/// </summary>
public sealed class QuizGraderTests
{
    // Deliberately not 0,1,2: Habbo's SafetyQuiz1 numbers its questions from zero and HabboWay1
    // does too, but nothing guarantees a hotel's own quiz has no gaps.
    private static readonly (int QuestionNumber, int CorrectAnswerIndex)[] Quiz =
    [
        (0, 2),
        (1, 1),
        (7, 3),
    ];

    [Fact]
    public void Grade_IsEmpty_OnAPerfectScore()
    {
        // Empty is the pass signal the client reads, not merely "no data".
        QuizGrader.Grade(Quiz, [2, 1, 3]).Should().BeEmpty();
    }

    [Fact]
    public void Grade_ReturnsTheQuestionNumbersNotThePositions()
    {
        // Getting the third question wrong must report 7, the localization key. Reporting 2 would
        // highlight a question the player answered correctly, or none at all.
        QuizGrader.Grade(Quiz, [2, 1, 0]).Should().Equal(7);
    }

    [Fact]
    public void Grade_ReportsEveryWrongAnswerInAskedOrder()
    {
        QuizGrader.Grade(Quiz, [0, 1, 0]).Should().Equal(0, 7);
    }

    [Fact]
    public void Grade_CountsMissingAnswersAsWrong()
    {
        // The client only submits once every question is answered, so a short array is a hand-made
        // packet. Treating the absent ones as passes would let anyone clear a quiz with an empty
        // submission.
        QuizGrader.Grade(Quiz, [2]).Should().Equal(1, 7);
    }

    [Fact]
    public void Grade_FailsEverythingOnAnEmptySubmission()
    {
        QuizGrader.Grade(Quiz, []).Should().Equal(0, 1, 7);
    }

    [Fact]
    public void Grade_IgnoresExtraAnswers()
    {
        // Trailing junk past the last question changes nothing: grading walks the questions, not
        // the answers.
        QuizGrader.Grade(Quiz, [2, 1, 3, 9, 9]).Should().BeEmpty();
    }

    [Fact]
    public void Grade_IsEmpty_ForAQuizWithNoQuestions()
    {
        List<(int, int)> none = [];

        QuizGrader.Grade(none, [1, 2, 3]).Should().BeEmpty();
    }
}
