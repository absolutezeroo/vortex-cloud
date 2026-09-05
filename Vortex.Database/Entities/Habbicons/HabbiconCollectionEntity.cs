using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Habbicons;

/// <summary>
/// A Habbicon set. The row's own id is the <c>collectionId</c> the client sees, and
/// <see cref="Code"/> is the localization stem it renders (<c>habbicon_collection_{code}_name</c>).
/// </summary>
/// <remarks>
/// The set's bonus Habbicon is not a column here: it is a <see cref="HabbiconEntity"/> in this
/// collection with <c>is_collection_reward</c> set. One place defines what a Habbicon is, and a
/// bonus is a Habbicon.
/// </remarks>
[Table("habbicon_collections")]
[Index(nameof(Code), IsUnique = true)]
public class HabbiconCollectionEntity : VortexEntity
{
    [Column("code")]
    [MaxLength(ContentIdLength)]
    public required string Code { get; set; }

    [Column("sort_order")]
    [DefaultValue(0)]
    public int SortOrder { get; set; }

    [Column("enabled")]
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    /// <summary>Served only to a player who already owns something in it.</summary>
    [Column("hidden")]
    [DefaultValue(false)]
    public bool Hidden { get; set; }

    [Column("available_from")]
    public DateTime? AvailableFrom { get; set; }

    [Column("available_until")]
    public DateTime? AvailableUntil { get; set; }

    /// <summary>Price for every entry the player is still missing. 0 = not sold as a set.</summary>
    [Column("price_credits")]
    [DefaultValue(0)]
    public int PriceCredits { get; set; }

    [Column("price_activity_points")]
    [DefaultValue(0)]
    public int PriceActivityPoints { get; set; }

    /// <summary>Which activity-point currency the price above is in (0 duckets, 5 diamonds, …).</summary>
    [Column("activity_point_type")]
    [DefaultValue(0)]
    public int ActivityPointType { get; set; }

    [Column("campaign_code")]
    [DefaultValue("")]
    public string CampaignCode { get; set; } = string.Empty;
}
