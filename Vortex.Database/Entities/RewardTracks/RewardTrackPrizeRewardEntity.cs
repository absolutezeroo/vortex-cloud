using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Primitives.RewardTracks;

namespace Vortex.Database.Entities.RewardTracks;

/// <summary>
/// One thing a prize hands over. A prize with several of these is a bundle — "100 duckets, a badge
/// and a Habbicon" is three rows, granted together under one claim.
/// </summary>
/// <remarks>
/// A row rather than a JSON blob on the prize so the dashboard can edit one reward of a bundle, and
/// so a report can ask which prizes hand out a given badge.
/// </remarks>
[Table("reward_track_prize_rewards")]
[Index(nameof(RewardTrackPrizeEntityId))]
public class RewardTrackPrizeRewardEntity : VortexEntity
{
    [Column("prize_id")]
    public required int RewardTrackPrizeEntityId { get; set; }

    /// <summary>
    /// The client's own product-type numbering; the serializer writes it out as the
    /// <c>productItemTypeId</c> short with no translation.
    /// </summary>
    [Column("kind")]
    public required RewardKind Kind { get; set; }

    /// <summary>
    /// What the kind names: a furniture id, a badge code, an activity-point type, an entitlement
    /// key. A string because half the kinds are not numbers, and because that is the client's own
    /// field type.
    /// </summary>
    [Column("reward_type_id")]
    public required string RewardTypeId { get; set; }

    [Column("amount")]
    [DefaultValue(1)]
    public int Amount { get; set; } = 1;

    /// <summary>Figure strings for bots and pets, extra data for furniture. Empty otherwise.</summary>
    [Column("extra_params")]
    [DefaultValue("")]
    public string ExtraParams { get; set; } = string.Empty;

    /// <summary>The first reward of a bundle is the one the client draws for the prize.</summary>
    [Column("sort_order")]
    [DefaultValue(0)]
    public int SortOrder { get; set; }

    [ForeignKey(nameof(RewardTrackPrizeEntityId))]
    public RewardTrackPrizeEntity? Prize { get; set; }
}
