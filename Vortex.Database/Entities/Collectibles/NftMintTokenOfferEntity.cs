using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Collectibles;

/// <summary>
/// A bundle of stamps for sale, priced in silver.
/// </summary>
/// <remarks>
/// Stamps are what a conversion is paid with, and silver is what stamps are bought with — both are
/// this hotel's own currencies, which is the whole reason minting works here. The client identifies
/// the bundle by this row's id when it buys, unlike the shop, which sends a classname.
/// </remarks>
[Table("nft_mint_token_offers")]
public class NftMintTokenOfferEntity : VortexEntity
{
    /// <summary>
    /// What the purchase dialog looks the bundle's name up under. It is a localization key rather
    /// than a classname — nothing is delivered from the catalogue here — so an unknown value is
    /// displayed raw instead of failing.
    /// </summary>
    [Column("product_code")]
    [MaxLength(128)]
    public required string ProductCode { get; set; }

    [Column("silver_price")]
    public int SilverPrice { get; set; }

    /// <summary>How many stamps the bundle hands over. This is also the label in the tab's dropdown,
    /// which lists bundles by their amount.</summary>
    [Column("amount_tokens")]
    public int AmountTokens { get; set; }

    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    [Column("sort_order")]
    public int SortOrder { get; set; }
}
