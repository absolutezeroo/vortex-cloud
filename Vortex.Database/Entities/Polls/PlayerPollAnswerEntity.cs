using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;

namespace Vortex.Database.Entities.Polls;

/// <summary>
/// One answer a player gave to one poll question. A multiple-choice question produces one row per
/// picked choice, which is what makes per-choice counting a plain <c>GROUP BY</c> for the dashboard.
/// </summary>
[Table("player_poll_answers")]
[Index(nameof(PlayerEntityId), nameof(PollEntityId))]
[Index(nameof(QuestionEntityId))]
public class PlayerPollAnswerEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("poll_id")]
    public required int PollEntityId { get; set; }

    [Column("question_id")]
    public required int QuestionEntityId { get; set; }

    /// <summary>
    /// The picked choice's <see cref="PollQuestionChoiceEntity.Value"/>, or the typed text for a
    /// free-text question.
    /// </summary>
    [Column("answer")]
    public required string Answer { get; set; }

    [Column("answered_at")]
    public required DateTime AnsweredAt { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }

    [ForeignKey(nameof(PollEntityId))]
    public PollEntity? PollEntity { get; set; }

    [ForeignKey(nameof(QuestionEntityId))]
    public PollQuestionEntity? QuestionEntity { get; set; }
}
