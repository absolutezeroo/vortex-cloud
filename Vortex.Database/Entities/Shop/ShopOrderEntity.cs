using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Primitives.Shop;

namespace Vortex.Database.Entities.Shop;

/// <summary>
/// One attempt to buy one product: what was agreed, who agreed to it, which provider is collecting,
/// and how far it got.
/// </summary>
/// <remarks>
/// <para>
/// Every money field is a COPY of the product's, taken when the order was opened. That is the whole
/// of the price-tampering defence on the reading side: the webhook checks the amount the provider
/// says it captured against this row, not against a product that may have been repriced in between,
/// and an order therefore means exactly one thing forever.
/// </para>
/// <para>
/// Two identities, deliberately. <see cref="VortexEntity.Id"/> is the internal one, and it is what
/// <c>CommerceOperationId.Deterministic</c> derives the fulfilment operation from — the same order
/// always maps to the same operation, which is what makes a replayed webhook a repeat rather than a
/// second purchase. <see cref="PublicId"/> is what leaves the building: an auto-increment in a URL
/// tells a stranger how many orders the hotel has taken and lets them count up through them.
/// </para>
/// </remarks>
[Table("shop_orders")]
[Index(nameof(PublicId), IsUnique = true)]
[Index(nameof(PlayerEntityId), nameof(Id))]
// The replay guard at the storage layer. MySQL permits many NULLs under a unique index, which is
// exactly the shape wanted: an order that has not been paid yet carries no reference and does not
// collide with the other unpaid ones, while two notifications quoting one payment cannot both land.
[Index(nameof(Provider), nameof(ProviderReference), IsUnique = true)]
[Index(nameof(State))]
public class ShopOrderEntity : VortexEntity
{
    /// <summary>The id the website and the provider use. Opaque, unguessable, not a row number.</summary>
    [Column("public_id")]
    public Guid PublicId { get; set; }

    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    /// <summary>The product's code, copied. A deleted product must not orphan a paid order.</summary>
    [Column("product_code")]
    [MaxLength(ContentIdLength)]
    public required string ProductCode { get; set; }

    [Column("kind")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required ShopProductKind Kind { get; set; }

    [Column("amount")]
    public required int Amount { get; set; }

    [Column("price_minor")]
    public required int PriceMinor { get; set; }

    [Column("currency")]
    [MaxLength(3)]
    public required string Currency { get; set; }

    [Column("state")]
    [DefaultValue(ShopOrderState.Pending)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ShopOrderState State { get; set; } = ShopOrderState.Pending;

    /// <summary><see cref="IShopPaymentProvider.Key"/>, copied so a retired integration stays readable.</summary>
    [Column("provider")]
    [MaxLength(32)]
    public required string Provider { get; set; }

    /// <summary>The provider's own id for the payment. Null until one is captured.</summary>
    [Column("provider_reference")]
    [MaxLength(128)]
    public string? ProviderReference { get; set; }

    /// <summary>When the provider told us the money was captured — the pivot.</summary>
    [Column("paid_at")]
    public DateTime? PaidAt { get; set; }

    /// <summary>When the goods actually reached the account.</summary>
    [Column("fulfilled_at")]
    public DateTime? FulfilledAt { get; set; }
}
