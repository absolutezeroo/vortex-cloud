using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Catalog;

/// <summary>
/// A targeted (personalised/promotional) catalog offer: a product bundle sold at a special price,
/// with a per-player purchase limit and an optional expiry. The grantable products live in
/// <see cref="Products"/>; per-player purchase counts / tracking live in
/// <see cref="PlayerTargetedOfferEntity"/>.
/// </summary>
[Table("targeted_offers")]
[Index(nameof(Identifier))]
public class TargetedOfferEntity : VortexEntity
{
    /// <summary>Client-facing offer code (wire <c>identifier</c>).</summary>
    [Column("identifier")]
    public required string Identifier { get; set; }

    /// <summary>Client display/layout type.</summary>
    [Column("offer_type")]
    [DefaultValue(0)]
    public int OfferType { get; set; }

    [Column("title")]
    [DefaultValue("")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    [DefaultValue("")]
    public string Description { get; set; } = string.Empty;

    [Column("image_url")]
    [DefaultValue("")]
    public string ImageUrl { get; set; } = string.Empty;

    [Column("icon_image_url")]
    [DefaultValue("")]
    public string IconImageUrl { get; set; } = string.Empty;

    /// <summary>Main product code shown to the client (wire <c>productCode</c>).</summary>
    [Column("product_code")]
    [DefaultValue("")]
    public string ProductCode { get; set; } = string.Empty;

    [Column("price_credits")]
    [DefaultValue(0)]
    public int PriceInCredits { get; set; }

    [Column("price_activity_points")]
    [DefaultValue(0)]
    public int PriceInActivityPoints { get; set; }

    [Column("activity_point_type")]
    [DefaultValue(0)]
    public int ActivityPointType { get; set; }

    /// <summary>How many times a single player may buy this offer (0 = unlimited).</summary>
    [Column("purchase_limit")]
    [DefaultValue(1)]
    public int PurchaseLimit { get; set; }

    /// <summary>Absolute expiry; the wire seconds-left is <c>ExpiresAt - now</c> (null = no expiry).</summary>
    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    [Column("active")]
    [DefaultValue(true)]
    public bool Active { get; set; } = true;

    [Column("sort_order")]
    [DefaultValue(0)]
    public int SortOrder { get; set; }

    [InverseProperty(nameof(TargetedOfferProductEntity.TargetedOfferEntity))]
    public List<TargetedOfferProductEntity>? Products { get; set; }
}
