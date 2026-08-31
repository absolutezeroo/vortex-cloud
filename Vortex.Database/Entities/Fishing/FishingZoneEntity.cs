using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Fishing;

/// <summary>
/// One fishing zone: a spot furni class, the level needed to fish it, and how much a spot holds.
/// </summary>
/// <remarks>
/// <para>
/// Keyed by furni class rather than by item id, so every copy of a spot behaves the same and a room
/// owner placing a second one changes nothing.
/// </para>
/// <para>
/// <strong>A spot depletes.</strong> Origins runs fishing as a session: the avatar fishes on its own
/// until the spot runs dry after an unpredictable number of catches, then the player relocates.
/// <see cref="MinCatches"/> and <see cref="MaxCatches"/> are what a fresh spot's stock is rolled
/// between; equal values give a fixed stock.
/// </para>
/// </remarks>
[Table("fishing_zones")]
[Index(nameof(FurniClass), IsUnique = true)]
public class FishingZoneEntity : VortexEntity
{
    /// <summary>A localisation key, never a display string.</summary>
    [Column("name_key")]
    [MaxLength(128)]
    public required string NameKey { get; set; }

    [Column("furni_class")]
    [MaxLength(128)]
    public required string FurniClass { get; set; }

    /// <summary>Zero means everybody.</summary>
    [Column("required_level")]
    public int RequiredLevel { get; set; }

    [Column("min_catches")]
    public int MinCatches { get; set; } = 1;

    [Column("max_catches")]
    public int MaxCatches { get; set; } = 5;
}
