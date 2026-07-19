using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Groups;

[Table("group_colors")]
[Index(nameof(ColorId), IsUnique = true)]
public class GroupColorEntity : VortexEntity
{
    /// <summary>Arcturus-compatible color id (stored in GroupEntity.ColorOne / ColorTwo).</summary>
    [Column("color_id")]
    public required int ColorId { get; set; }

    [Column("color_hex")]
    public required string ColorHex { get; set; }
}
