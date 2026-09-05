using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Primitives.RewardTracks;

namespace Vortex.Database.Entities.RewardTracks;

/// <summary>
/// A reward track. <see cref="TrackId"/> — not the row's numeric id — is the identity everything
/// else uses: the client sends it back on a claim, player rows key on it, and localization is built
/// from it. That is deliberate, so a track can be rebuilt in place without orphaning a single
/// player's progress.
/// </summary>
[Table("reward_tracks")]
[Index(nameof(TrackId), IsUnique = true)]
[Index(nameof(Status))]
public class RewardTrackEntity : VortexEntity
{
    /// <summary>Content id: <c>introduction</c>, <c>summer_2026</c>. Localization stem and wire identity.</summary>
    [Column("track_id")]
    [MaxLength(ContentIdLength)]
    public required string TrackId { get; set; }

    /// <summary>
    /// One of the client's palettes — <c>blue</c>, <c>orange</c>, <c>forest_green</c>, <c>red</c>,
    /// <c>cyan</c>. Anything else renders blue rather than failing.
    /// </summary>
    [Column("theme")]
    [DefaultValue("blue")]
    public string Theme { get; set; } = "blue";

    [Column("status")]
    [DefaultValue(RewardTrackStatus.Draft)]
    public RewardTrackStatus Status { get; set; } = RewardTrackStatus.Draft;

    [Column("sort_order")]
    [DefaultValue(0)]
    public int SortOrder { get; set; }

    [Column("starts_at")]
    public DateTime? StartsAt { get; set; }

    /// <summary>Tasks stop advancing here. Null = never.</summary>
    [Column("progress_ends_at")]
    public DateTime? ProgressEndsAt { get; set; }

    /// <summary>
    /// Claiming closes here, and with it the track. Kept separate from
    /// <see cref="ProgressEndsAt"/> so a campaign can stop counting on the last day and still let
    /// people collect what they earned.
    /// </summary>
    [Column("claim_ends_at")]
    public DateTime? ClaimEndsAt { get; set; }

    [Column("unlock_kind")]
    [DefaultValue(RewardTrackUnlockKind.Always)]
    public RewardTrackUnlockKind UnlockKind { get; set; } = RewardTrackUnlockKind.Always;

    [Column("unlock_value")]
    [DefaultValue("")]
    public string UnlockValue { get; set; } = string.Empty;

    [Column("completion_policy")]
    [DefaultValue(RewardTrackCompletionPolicy.AllFreePrizesClaimed)]
    public RewardTrackCompletionPolicy CompletionPolicy { get; set; } =
        RewardTrackCompletionPolicy.AllFreePrizesClaimed;

    /// <summary>False leaves every premium column ignored and the track free-only.</summary>
    [Column("premium_enabled")]
    [DefaultValue(false)]
    public bool PremiumEnabled { get; set; }

    /// <summary>
    /// Task-points multiplier in per-mille: 1200 is 1.2×, which the client shows as "20% faster
    /// progression". Per-mille rather than a double so the grant is integer arithmetic end to end.
    /// </summary>
    [Column("premium_boost_permille")]
    [DefaultValue(1000)]
    public int PremiumBoostPerMille { get; set; } = 1000;

    [Column("premium_instant_points")]
    [DefaultValue(0)]
    public int PremiumInstantPoints { get; set; }

    [Column("premium_cost_credits")]
    [DefaultValue(0)]
    public int PremiumCostCredits { get; set; }

    [Column("premium_cost_diamonds")]
    [DefaultValue(0)]
    public int PremiumCostDiamonds { get; set; }

    /// <summary>
    /// Bumped on every structural edit. A player whose stored row carries an older number is pushed
    /// the track list with the client's <c>reload</c> flag set.
    /// </summary>
    [Column("content_version")]
    [DefaultValue(1)]
    public int ContentVersion { get; set; } = 1;

    /// <summary>Served only to a player who already has progress on it.</summary>
    [Column("hidden")]
    [DefaultValue(false)]
    public bool Hidden { get; set; }

    [Column("campaign_code")]
    [DefaultValue("")]
    public string CampaignCode { get; set; } = string.Empty;
}
