using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Collectibles;

/// <summary>
/// One movement of one Relic: who had it, who has it now, and why.
/// </summary>
/// <remarks>
/// <para>
/// This is the part of a blockchain that was actually worth having. A chain is, for our purposes, a
/// public ledger that says where a collectible came from — and a table says it just as well on a
/// hotel where every participant is a row in <c>players</c> anyway.
/// </para>
/// <para>
/// Append-only by convention: a movement that turns out to be wrong is corrected by another
/// movement, not by editing this one. That is what makes the history worth reading.
/// </para>
/// </remarks>
[Table("nft_asset_ledger")]
[Index(nameof(NftAssetEntityId))]
public class NftAssetLedgerEntity : VortexEntity
{
    [Column("nft_asset_id")]
    public required int NftAssetEntityId { get; set; }

    /// <summary>Who held it before. Null when the Relic came into existence here — a mint has no
    /// previous owner, and that is the first line of every history.</summary>
    [Column("from_player_id")]
    public int? FromPlayerEntityId { get; set; }

    [Column("to_player_id")]
    public required int ToPlayerEntityId { get; set; }

    /// <summary>What moved it. Kept as text rather than an enum column so a new reason costs a
    /// string, not a migration — the value is read by people, not branched on.</summary>
    [Column("reason")]
    [MaxLength(32)]
    public required string Reason { get; set; }

    [ForeignKey(nameof(NftAssetEntityId))]
    public NftAssetEntity? NftAssetEntity { get; set; }
}

/// <summary>The reasons a Relic changes hands. Named here so the strings agree across writers.</summary>
public static class NftAssetLedgerReason
{
    /// <summary>Converted from furniture the player owned. Always the first line.</summary>
    public const string Minted = "minted";

    /// <summary>Changed hands in a trade.</summary>
    public const string Traded = "traded";
}
