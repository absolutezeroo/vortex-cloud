using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.MysteryBox;

namespace Vortex.Database.Entities.MysteryBox;

/// <summary>
/// One entry of a prize pool. <see cref="Pool"/> separates the mystery box pool from the mystery
/// trophy pool; <see cref="Color"/> (empty = any) further narrows a box prize to a single box
/// colour. <see cref="Weight"/> is the relative draw chance, so tuning the odds is a database edit
/// rather than a redeploy.
/// </summary>
[Table("mystery_box_prizes")]
[Index(nameof(Pool), nameof(Enabled))]
public class MysteryBoxPrizeEntity : VortexEntity
{
    [Column("pool")]
    [DefaultValue(MysteryBoxPrizePool.Box)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required MysteryBoxPrizePool Pool { get; set; }

    /// <summary>Box colour this prize is restricted to; empty means it can drop from any colour.</summary>
    [Column("color")]
    [MaxLength(32)]
    [DefaultValue("")]
    public string Color { get; set; } = string.Empty;

    /// <summary>What is granted. Only <see cref="ProductType.Floor"/>, <see cref="ProductType.Wall"/>,
    /// <see cref="ProductType.Effect"/> and <see cref="ProductType.HabboClub"/> can be drawn by the
    /// reward window; other types are ignored by the client and rejected at load time.</summary>
    [Column("product_type")]
    [DefaultValue(ProductType.Floor)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required ProductType ProductType { get; set; }

    /// <summary>Furniture definition granted for floor/wall prizes (0 for the other types).</summary>
    [Column("furniture_definition_id")]
    [DefaultValue(0)]
    public int FurnitureDefinitionEntityId { get; set; }

    /// <summary>Effect prizes: <c>effectId[:durationSeconds[:subType]]</c>. Club prizes: number of
    /// months. Unused for floor/wall prizes.</summary>
    [Column("extra_param")]
    [MaxLength(128)]
    [DefaultValue("")]
    public string ExtraParam { get; set; } = string.Empty;

    [Column("weight")]
    [DefaultValue(1)]
    public int Weight { get; set; } = 1;

    [Column("enabled")]
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;
}
