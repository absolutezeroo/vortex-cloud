using System.ComponentModel.DataAnnotations.Schema;

namespace Vortex.Database.Entities.Players;

[Table("player_navigator_saved_searches")]
public class PlayerNavigatorSavedSearchEntity : TurboEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("search_code")]
    public required string SearchCode { get; set; }

    [Column("filter")]
    public string Filter { get; set; } = string.Empty;

    [Column("order_num")]
    public int OrderNum { get; set; } = 0;

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }
}
