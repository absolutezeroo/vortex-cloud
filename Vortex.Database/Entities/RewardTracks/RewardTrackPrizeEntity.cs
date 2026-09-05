using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.RewardTracks;

/// <summary>
/// A milestone on a track. It knows a points threshold and what it hands over, and nothing about
/// which task paid for the points — that separation is what lets tasks and rewards be edited
/// independently.
/// </summary>
[Table("reward_track_prizes")]
[Index(nameof(RewardTrackEntityId), nameof(PrizeId), IsUnique = true)]
[Index(nameof(RewardTrackEntityId), nameof(RequiredPoints))]
public class RewardTrackPrizeEntity : VortexEntity
{
    [Column("reward_track_id")]
    public required int RewardTrackEntityId { get; set; }

    /// <summary>
    /// Content id, unique within the track, and the identity a claim is recorded against for good.
    /// Changing what a prize hands out is allowed; changing this id is a new prize, and the old
    /// claims stay attached to the old one.
    /// </summary>
    [Column("prize_id")]
    [MaxLength(ContentIdLength)]
    public required string PrizeId { get; set; }

    [Column("required_points")]
    public required int RequiredPoints { get; set; }

    [Column("premium")]
    [DefaultValue(false)]
    public bool Premium { get; set; }

    [Column("sort_order")]
    [DefaultValue(0)]
    public int SortOrder { get; set; }

    [ForeignKey(nameof(RewardTrackEntityId))]
    public RewardTrackEntity? RewardTrack { get; set; }
}
