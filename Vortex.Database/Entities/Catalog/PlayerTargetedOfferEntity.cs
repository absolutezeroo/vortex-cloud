using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;

namespace Vortex.Database.Entities.Catalog;

/// <summary>
/// A player's state for one targeted offer: how many times they have bought it (enforces the
/// per-player purchase limit) and the last tracking state the client reported.
/// </summary>
[Table("player_targeted_offers")]
[Index(nameof(PlayerEntityId), nameof(TargetedOfferEntityId), IsUnique = true)]
public class PlayerTargetedOfferEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("targeted_offer_id")]
    public required int TargetedOfferEntityId { get; set; }

    [Column("purchase_count")]
    [DefaultValue(0)]
    public int PurchaseCount { get; set; }

    [Column("tracking_state")]
    [DefaultValue(0)]
    public int TrackingState { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }

    [ForeignKey(nameof(TargetedOfferEntityId))]
    public TargetedOfferEntity? TargetedOfferEntity { get; set; }
}
