using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Primitives.Polls;

namespace Vortex.Database.Entities.Polls;

/// <summary>
/// One question of a <see cref="PollEntity"/>. A row with no <see cref="ParentQuestionEntityId"/>
/// is a root question — those are the ones counted on the wire and the ones a player must answer
/// for the poll to count as completed. A row with a parent is an NPS follow-up, shown only when
/// the parent's picked choice carries a matching <see cref="QuestionCategory"/>.
/// </summary>
[Table("poll_questions")]
[Index(nameof(PollEntityId))]
[Index(nameof(ParentQuestionEntityId))]
public class PollQuestionEntity : VortexEntity
{
    [Column("poll_id")]
    public required int PollEntityId { get; set; }

    /// <summary>Null for a root question; otherwise the root question this one follows up on.</summary>
    [Column("parent_question_id")]
    public int? ParentQuestionEntityId { get; set; }

    [Column("sort_order")]
    [DefaultValue(0)]
    public int SortOrder { get; set; }

    [Column("question_type")]
    public required PollQuestionType QuestionType { get; set; }

    [Column("question_text")]
    public required string QuestionText { get; set; }

    /// <summary>
    /// Branch key. On a child question it must equal the <see cref="PollQuestionChoiceEntity.ChoiceType"/>
    /// of the parent choice that should lead here; 0 on a root question.
    /// </summary>
    [Column("question_category")]
    [DefaultValue(0)]
    public int QuestionCategory { get; set; }

    /// <summary>Client-side answer-format hint, passed straight through on the wire.</summary>
    [Column("question_answer_type")]
    [DefaultValue(0)]
    public int QuestionAnswerType { get; set; }

    [ForeignKey(nameof(PollEntityId))]
    public PollEntity? PollEntity { get; set; }

    [ForeignKey(nameof(ParentQuestionEntityId))]
    public PollQuestionEntity? ParentQuestionEntity { get; set; }
}
