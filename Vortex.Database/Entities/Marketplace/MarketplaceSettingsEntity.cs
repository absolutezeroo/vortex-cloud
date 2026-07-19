using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vortex.Database.Entities.Marketplace;

/// <summary>
/// Singleton row (the first/only row in the table) holding admin-editable marketplace tunables.
/// Defaults match the values previously hardcoded in <c>MarketplacePurchaseGrain</c> and
/// <c>GetMarketplaceConfigurationMessageHandler</c>, so seeding this row changes nothing until an
/// operator edits it.
/// </summary>
[Table("marketplace_settings")]
public class MarketplaceSettingsEntity : TurboEntity
{
    [Column("commission_percent")]
    [DefaultValue(1)]
    public int CommissionPercent { get; set; } = 1;

    [Column("offer_duration_seconds")]
    [DefaultValue(259200)]
    public int OfferDurationSeconds { get; set; } = 259200;
}
