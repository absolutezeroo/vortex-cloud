using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vortex.Database.Entities.Navigator;

[Table("navigator_eventcats")]
public class NavigatorEventCategoryEntity : VortexEntity
{
    [Key]
    [Column("id")]
    public new int Id { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [Column("visible")]
    public required bool Visible { get; set; }
}
