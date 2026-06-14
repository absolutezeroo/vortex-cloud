using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Turbo.Primitives.Navigator.Enums;

namespace Turbo.Database.Entities.Navigator;

[Table("navigator_quick_links")]
public class NavigatorQuickLinkEntity : TurboEntity
{
    [Column("top_level_context_id")]
    public required int TopLevelContextEntityId { get; set; }

    [Column("search_code")]
    public required string SearchCode { get; set; }

    [Column("filter")]
    public string Filter { get; set; } = string.Empty;

    [Column("localization")]
    public string Localization { get; set; } = string.Empty;

    [Column("query_type")]
    [DefaultValue(NavigatorQueryType.AllRooms)]
    public NavigatorQueryType QueryType { get; set; } = NavigatorQueryType.AllRooms;

    [Column("order_num")]
    [DefaultValue(0)]
    public int OrderNum { get; set; } = 0;

    [ForeignKey(nameof(TopLevelContextEntityId))]
    public NavigatorTopLevelContextEntity? TopLevelContextEntity { get; set; }
}
