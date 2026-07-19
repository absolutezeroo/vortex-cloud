using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Primitives.Rooms.Enums.Wired;

namespace Vortex.Database.Entities.Wired;

[Table("wired_permanent_variables")]
[Index(nameof(TargetType), nameof(TargetId), nameof(VariableId), IsUnique = true)]
public class WiredPermanentVariableEntity : VortexEntity
{
    [Column("target_type")]
    public required WiredVariableTargetType TargetType { get; set; }

    [Column("target_id")]
    public required int TargetId { get; set; }

    [Column("variable_id")]
    [MaxLength(128)]
    public required string VariableId { get; set; }

    [Column("value")]
    public required int Value { get; set; }
}
