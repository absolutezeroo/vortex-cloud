using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Turbo.Primitives.Permissions;

namespace Turbo.Database.Entities.Permissions;

/// <summary>
/// Admin-manageable, named sanction option shown in the staff CFH tool's ModBan/ModTradingLock
/// dropdowns. <see cref="PresetIndex"/> matches the client-sent sanctionTypeId/lockDurationTypeId —
/// the client picks by position from its own local list, so the (Kind, PresetIndex) pair is how the
/// server maps that back to a real, tunable duration instead of a hardcoded number.
/// </summary>
[Table("sanction_presets")]
[Index(nameof(Kind), nameof(PresetIndex), IsUnique = true)]
public class SanctionPresetEntity : TurboEntity
{
    [Column("kind")]
    public required SanctionPresetKind Kind { get; set; }

    [Column("preset_index")]
    public required int PresetIndex { get; set; }

    [Column("name")]
    [MaxLength(100)]
    public required string Name { get; set; }

    /// <summary>Null = permanent.</summary>
    [Column("duration_seconds")]
    public int? DurationSeconds { get; set; }

    [Column("message")]
    [MaxLength(255)]
    public string? Message { get; set; }
}
