using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Help;

/// <summary>
/// One question, which is only ever a number and an answer key: the text lives in the client's
/// localization, never on the wire.
/// </summary>
[Table("quiz_questions")]
[Index(nameof(QuizEntityId), nameof(QuestionNumber), IsUnique = true)]
public class QuizQuestionEntity : VortexEntity
{
    [Column("quiz_id")]
    public required int QuizEntityId { get; set; }

    /// <summary>The number in <c>quiz.&lt;code&gt;.question.&lt;n&gt;</c>. Habbo's own quizzes start
    /// at zero, so this is not a 1-based position and must not be renumbered to look tidy.</summary>
    [Column("question_number")]
    public required int QuestionNumber { get; set; }

    /// <summary>Index into <c>quiz.&lt;code&gt;.answer.&lt;n&gt;.&lt;i&gt;</c>. The client shuffles
    /// how the options are shown but names each by this index, so it grades against the real
    /// answer and not a screen position.</summary>
    [Column("correct_answer_index")]
    public required int CorrectAnswerIndex { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [ForeignKey(nameof(QuizEntityId))]
    public QuizEntity? QuizEntity { get; set; }
}
