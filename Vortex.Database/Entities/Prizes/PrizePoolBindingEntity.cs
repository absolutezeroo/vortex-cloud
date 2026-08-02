using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Prizes;

/// <summary>
/// Points a furniture definition at the pool it draws from, so which egg pays out which prizes is an
/// operator row rather than a code change. Kept here rather than as columns on
/// <c>furniture_definitions</c>: that table is shared by every furniture in the hotel, and a binding
/// only means anything to the handful that hand something out.
/// </summary>
[Table("prize_pool_bindings")]
[Index(nameof(FurnitureDefinitionEntityId), IsUnique = true)]
public class PrizePoolBindingEntity : VortexEntity
{
    [Column("furniture_definition_id")]
    public required int FurnitureDefinitionEntityId { get; set; }

    [Column("pool_id")]
    public required int PrizePoolEntityId { get; set; }

    /// <summary>
    /// Hits this furniture takes before it pays out. One means a single click, which is what the
    /// reward boxes use; crackables ship higher counts. Nothing in furnidata carries this — the
    /// client only ever renders the counters the server sends — so it has to live here.
    /// </summary>
    [Column("hits_required")]
    [DefaultValue(1)]
    public int HitsRequired { get; set; } = 1;

    [Column("enabled")]
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    [ForeignKey(nameof(PrizePoolEntityId))]
    public PrizePoolEntity? PrizePoolEntity { get; set; }
}
