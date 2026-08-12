using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Polls;

/// <summary>
/// A selectable answer of a <see cref="PollQuestionEntity"/>. Only questions of type
/// <c>SingleChoice</c> / <c>MultipleChoice</c> carry choices — the client reads the choice list
/// only for those two types.
/// </summary>
[Table("poll_question_choices")]
[Index(nameof(QuestionEntityId))]
public class PollQuestionChoiceEntity : VortexEntity
{
    [Column("question_id")]
    public required int QuestionEntityId { get; set; }

    /// <summary>What the client sends back as the answer when this choice is picked.</summary>
    [Column("value")]
    public required string Value { get; set; }

    /// <summary>Label rendered next to the radio button / checkbox.</summary>
    [Column("choice_text")]
    public required string ChoiceText { get; set; }

    /// <summary>
    /// Branch key for NPS polls: picking this choice makes the client look for a follow-up question
    /// whose <see cref="PollQuestionEntity.QuestionCategory"/> equals this value. 0 = no follow-up.
    /// </summary>
    [Column("choice_type")]
    [DefaultValue(0)]
    public int ChoiceType { get; set; }

    [Column("sort_order")]
    [DefaultValue(0)]
    public int SortOrder { get; set; }

    [ForeignKey(nameof(QuestionEntityId))]
    public PollQuestionEntity? QuestionEntity { get; set; }
}
