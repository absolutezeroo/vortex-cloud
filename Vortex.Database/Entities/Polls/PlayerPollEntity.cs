using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Polls;

namespace Vortex.Database.Entities.Polls;

/// <summary>
/// Where one player stands on one poll. A row exists as soon as the poll has been offered, so a
/// declined or finished poll is never offered twice.
/// </summary>
[Table("player_polls")]
[Index(nameof(PlayerEntityId), nameof(PollEntityId), IsUnique = true)]
public class PlayerPollEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("poll_id")]
    public required int PollEntityId { get; set; }

    [Column("state")]
    public required PollParticipationState State { get; set; }

    [Column("offered_at")]
    public DateTime? OfferedAt { get; set; }

    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    [Column("finished_at")]
    public DateTime? FinishedAt { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }

    [ForeignKey(nameof(PollEntityId))]
    public PollEntity? PollEntity { get; set; }
}
