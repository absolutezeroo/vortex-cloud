using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;

namespace Vortex.Database.Entities.Help;

/// <summary>
/// A player's standing on one quiz. Kept even after a pass, because the badge is granted once and
/// the attempt count is the only record that a quiz was ever failed — the client shows the review
/// screen and moves on without telling the server anything more.
/// </summary>
[Table("player_quizzes")]
[Index(nameof(PlayerEntityId), nameof(QuizEntityId), IsUnique = true)]
public class PlayerQuizEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("quiz_id")]
    public required int QuizEntityId { get; set; }

    [Column("attempts")]
    public int Attempts { get; set; }

    [Column("passed_at")]
    public DateTime? PassedAt { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }

    [ForeignKey(nameof(QuizEntityId))]
    public QuizEntity? QuizEntity { get; set; }
}
