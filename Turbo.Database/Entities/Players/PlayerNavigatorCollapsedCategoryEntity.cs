using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Turbo.Database.Entities.Players;

[Table("player_navigator_collapsed_categories")]
[Index(nameof(PlayerEntityId), nameof(CategoryName), IsUnique = true)]
public class PlayerNavigatorCollapsedCategoryEntity : TurboEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("category_name")]
    public required string CategoryName { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }
}
