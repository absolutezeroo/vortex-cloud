using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;

namespace Vortex.Database.Entities.RewardTracks;

/// <summary>
/// One player's standing on one track: their points, whether they hold premium on it, and whether
/// they have finished it.
/// </summary>
/// <remarks>
/// Keyed on the track's <em>content</em> id rather than a foreign key to the definition row. A
/// campaign that is deleted and rebuilt keeps its players' history; a cascade would have deleted
/// it. That does mean nothing at the database level stops an orphan, which is what the content
/// validator's "player rows on an unknown track" check is for.
/// </remarks>
[Table("player_reward_tracks")]
[Index(nameof(PlayerEntityId), nameof(TrackId), IsUnique = true)]
[Index(nameof(TrackId))]
public class PlayerRewardTrackEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("track_id")]
    [MaxLength(ContentIdLength)]
    public required string TrackId { get; set; }

    /// <summary>Track points. Only ever goes up.</summary>
    [Column("points")]
    [DefaultValue(0)]
    public int Points { get; set; }

    /// <summary>Premium on <em>this</em> track. Buying it elsewhere does not set this.</summary>
    [Column("premium_unlocked")]
    [DefaultValue(false)]
    public bool PremiumUnlocked { get; set; }

    /// <summary>
    /// When premium started. The boost applies to points earned after this instant and never
    /// retroactively, so the moment is part of the record rather than a flag.
    /// </summary>
    [Column("premium_unlocked_at")]
    public DateTime? PremiumUnlockedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>The definition version this row was last reconciled against.</summary>
    [Column("content_version")]
    [DefaultValue(0)]
    public int ContentVersion { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }
}
