using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Fishing;

/// <summary>
/// One player's best catch of one species — the row behind the Fishopedia.
/// </summary>
/// <remarks>
/// <para>
/// Reconstructed from Habbo Origins, which has no client dump — see the client's
/// <c>docs/vortex-original/fishing.md</c>.
/// </para>
/// <para>
/// A row exists only once the species has been caught, so the client's book shows an entry as
/// undiscovered by its <em>absence</em> rather than by a flag. That keeps the table proportional to
/// what a player has done rather than to how many species the operator has defined.
/// </para>
/// </remarks>
[Table("fishing_records")]
[Index(nameof(PlayerId), nameof(SpeciesId), IsUnique = true)]
[Index(nameof(SpeciesId), nameof(BestWeight))]
public class FishingRecordEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerId { get; set; }

    [Column("species_id")]
    public required int SpeciesId { get; set; }

    /// <summary>Heaviest ever caught, in the simulation's own integer unit.</summary>
    [Column("best_weight")]
    public int BestWeight { get; set; }

    [Column("caught_count")]
    public int CaughtCount { get; set; }

    /// <summary>When <see cref="BestWeight"/> was set — not when the species was first caught.</summary>
    [Column("best_at")]
    public DateTime BestAt { get; set; }
}
