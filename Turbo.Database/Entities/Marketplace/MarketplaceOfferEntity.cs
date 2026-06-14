using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Turbo.Database.Entities.Furniture;
using Turbo.Database.Entities.Players;

namespace Turbo.Database.Entities.Marketplace;

[Table("marketplace_offers")]
public class MarketplaceOfferEntity : TurboEntity
{
    [Column("seller_id")]
    public int SellerEntityId { get; set; }

    [Column("definition_id")]
    public int FurnitureDefinitionEntityId { get; set; }

    [Column("sprite_id")]
    public int SpriteId { get; set; }

    [Column("furni_type")]
    [DefaultValue(1)]
    public int FurnitureType { get; set; } = 1;

    [Column("extra_data")]
    public string? ExtraData { get; set; }

    [Column("price")]
    public int Price { get; set; }

    [Column("state")]
    [DefaultValue(MarketplaceOfferState.Active)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public MarketplaceOfferState State { get; set; } = MarketplaceOfferState.Active;

    [Column("credits_owed")]
    [DefaultValue(0)]
    public int CreditsOwed { get; set; }

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [ForeignKey(nameof(SellerEntityId))]
    public PlayerEntity? SellerEntity { get; set; }

    [ForeignKey(nameof(FurnitureDefinitionEntityId))]
    public FurnitureDefinitionEntity? FurnitureDefinitionEntity { get; set; }
}
