using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Fishing;

/// <summary>
/// One scheduled fishing derby.
/// </summary>
/// <remarks>
/// <para>
/// Vortex's own addition, not an Origins feature: Origins has the Fishing Frenzy, not a leaderboard
/// contest. See the client's <c>docs/vortex-original/fishing.md</c>.
/// </para>
/// <para>
/// A row rather than a config value because an operator schedules these one at a time, and because
/// the standings have to survive a restart — a contest that resets when the hotel is patched is not
/// a contest.
/// </para>
/// </remarks>
[Table("fishing_derbies")]
[Index(nameof(StartsAt), nameof(EndsAt))]
public class FishingDerbyEntity : VortexEntity
{
    /// <summary>A localisation key, never a display string.</summary>
    [Column("name_key")]
    [MaxLength(128)]
    public required string NameKey { get; set; }

    [Column("starts_at")]
    public required DateTime StartsAt { get; set; }

    [Column("ends_at")]
    public required DateTime EndsAt { get; set; }

    /// <summary>Zero means every zone.</summary>
    [Column("zone_id")]
    public int ZoneId { get; set; }
}
