using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Primitives.Shop;

namespace Vortex.Database.Entities.Shop;

/// <summary>
/// One thing the hotel sells for real money.
/// </summary>
/// <remarks>
/// <para>
/// In the database rather than in code because it is content: a bundle's price, its position in the
/// grid and whether it is on sale at all are decisions an operator makes on a Tuesday, and a
/// redeploy is not an acceptable price for changing one. It is also what makes the "the browser
/// never names a price" rule enforceable — there has to be somewhere authoritative to read the price
/// FROM.
/// </para>
/// <para>
/// <see cref="PriceMinor"/> is an integer count of the currency's minor unit (450 = 4,50 €), which is
/// the only representation of money that does not eventually lose a cent. <see cref="Currency"/> is
/// ISO 4217 and lives on the row rather than in config: a hotel that adds a second currency should
/// not have to migrate, and an order snapshots whichever one its product carried.
/// </para>
/// </remarks>
[Table("shop_products")]
[Index(nameof(Code), IsUnique = true)]
[Index(nameof(IsActive), nameof(Section), nameof(SortOrder))]
public class ShopProductEntity : VortexEntity
{
    /// <summary>The stable identifier the website posts. Never a database id: ids renumber.</summary>
    [Column("code")]
    [MaxLength(ContentIdLength)]
    public required string Code { get; set; }

    /// <summary>What it pays out. Decides which grain is called, and nothing else does.</summary>
    [Column("kind")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required ShopProductKind Kind { get; set; }

    /// <summary>How much of it: credits, duckets, diamonds — or months, for the two club kinds.</summary>
    [Column("amount")]
    public required int Amount { get; set; }

    /// <summary>The price in minor units. 450 is 4,50 €.</summary>
    [Column("price_minor")]
    public required int PriceMinor { get; set; }

    /// <summary>ISO 4217.</summary>
    [Column("currency")]
    [MaxLength(3)]
    public required string Currency { get; set; }

    /// <summary>Which heading of the store page it appears under.</summary>
    [Column("section")]
    [MaxLength(ContentIdLength)]
    public required string Section { get; set; }

    [Column("sort_order")]
    [DefaultValue(0)]
    public int SortOrder { get; set; }

    /// <summary>Which of the shop's product sprites the tile draws (1-6).</summary>
    [Column("icon")]
    [DefaultValue(1)]
    public int Icon { get; set; } = 1;

    /// <summary>The "best value" flag on the tile. At most one per section, by convention only.</summary>
    [Column("featured")]
    [DefaultValue(false)]
    public bool Featured { get; set; }

    /// <summary>
    /// Off takes it out of the catalogue AND refuses new orders for it. Orders already opened keep
    /// their own snapshot and are still fulfilled: withdrawing a product must not strand a payment
    /// somebody has already made.
    /// </summary>
    [Column("is_active")]
    [DefaultValue(true)]
    public bool IsActive { get; set; } = true;
}
