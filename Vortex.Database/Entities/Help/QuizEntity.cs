using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Help;

/// <summary>
/// A quiz the help windows can open. <see cref="Code"/> is not a label — the client sends it
/// verbatim ("HabboWay1", "SafetyQuiz1") and builds every question and answer from its own
/// localization under that code, so a code with no matching texts renders as blank questions.
/// </summary>
[Table("quizzes")]
[Index(nameof(Code), IsUnique = true)]
public class QuizEntity : VortexEntity
{
    [Column("code")]
    public required string Code { get; set; }

    /// <summary>Badge granted the first time the quiz is passed, or empty for none.</summary>
    [Column("reward_badge_code")]
    public string? RewardBadgeCode { get; set; }

    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    public List<QuizQuestionEntity>? Questions { get; set; }
}
