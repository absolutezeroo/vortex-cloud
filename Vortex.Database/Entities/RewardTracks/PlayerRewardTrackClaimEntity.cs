using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;

namespace Vortex.Database.Entities.RewardTracks;

/// <summary>
/// One prize, claimed once, by one player. The unique index is the claim's idempotency: the insert
/// and the grant share a transaction, so a second attempt loses the insert and rolls the grant back
/// with it.
/// </summary>
[Table("player_reward_track_claims")]
[Index(nameof(PlayerEntityId), nameof(TrackId), nameof(PrizeId), IsUnique = true)]
[Index(nameof(TrackId), nameof(PrizeId))]
public class PlayerRewardTrackClaimEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("track_id")]
    [MaxLength(ContentIdLength)]
    public required string TrackId { get; set; }

    [Column("prize_id")]
    [MaxLength(ContentIdLength)]
    public required string PrizeId { get; set; }

    [Column("claimed_at")]
    public required DateTime ClaimedAt { get; set; }

    /// <summary>Points the player held when they claimed. What made the claim legal, kept.</summary>
    [Column("points_at_claim")]
    [DefaultValue(0)]
    public int PointsAtClaim { get; set; }

    /// <summary>
    /// What was actually handed over, rendered at claim time — <c>currency:0x100</c>,
    /// <c>badge:ACH_Foo</c>. The prize definition can be rewritten afterwards; this cannot, so
    /// "why does this player have that?" stays answerable a year later.
    /// </summary>
    [Column("granted_summary")]
    [DefaultValue("")]
    public string GrantedSummary { get; set; } = string.Empty;

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }
}
