using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Habbicons;

/// <summary>
/// One Habbicon definition. The row's own id is the <c>habbiconId</c> on the wire, and it is also
/// the key the client looks the artwork up by in its <c>habbicons.json</c> manifest — so ids here
/// and ids in the client's asset pack are the same numbering, and renumbering a row swaps a
/// player's picture.
/// </summary>
[Table("habbicons")]
[Index(nameof(Code), IsUnique = true)]
[Index(nameof(HabbiconCollectionEntityId))]
public class HabbiconEntity : VortexEntity
{
    /// <summary>Asset and localization stem, e.g. <c>duck_01</c>.</summary>
    [Column("code")]
    [MaxLength(ContentIdLength)]
    public required string Code { get; set; }

    [Column("collection_id")]
    public required int HabbiconCollectionEntityId { get; set; }

    [Column("sort_order")]
    [DefaultValue(0)]
    public int SortOrder { get; set; }

    /// <summary>
    /// The collection's bonus Habbicon rather than one of its entries. Excluded from its own
    /// collection's completion check — counting it would make the set uncompletable.
    /// </summary>
    [Column("is_collection_reward")]
    [DefaultValue(false)]
    public bool IsCollectionReward { get; set; }

    [Column("price_credits")]
    [DefaultValue(0)]
    public int PriceCredits { get; set; }

    [Column("price_activity_points")]
    [DefaultValue(0)]
    public int PriceActivityPoints { get; set; }

    [Column("activity_point_type")]
    [DefaultValue(0)]
    public int ActivityPointType { get; set; }

    [Column("enabled")]
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    [Column("available_from")]
    public DateTime? AvailableFrom { get; set; }

    [Column("available_until")]
    public DateTime? AvailableUntil { get; set; }

    [ForeignKey(nameof(HabbiconCollectionEntityId))]
    public HabbiconCollectionEntity? Collection { get; set; }
}
