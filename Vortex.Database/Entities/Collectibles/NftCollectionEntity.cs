using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Collectibles;

/// <summary>
/// A set of furniture a hotel groups together so players can try to complete it. Unlike the rest of
/// the collectibles interface — minting, wallets, transfers — a collection needs no blockchain: it
/// is a list of classnames and a reward for owning them all.
/// </summary>
[Table("nft_collections")]
[Index(nameof(CollectionCode), IsUnique = true)]
public class NftCollectionEntity : VortexEntity
{
    /// <summary>The id the client knows the collection by; free text, and a hotel's to choose.</summary>
    [Column("collection_code")]
    [MaxLength(64)]
    public required string CollectionCode { get; set; }

    [Column("name")]
    [MaxLength(128)]
    public required string Name { get; set; }

    /// <summary>Added to a completed collection's score, on top of what its items are worth.</summary>
    [Column("boost_score")]
    public int BoostScore { get; set; }

    [Column("released_at")]
    public DateTime? ReleasedAt { get; set; }

    /// <summary>When ownership was last taken stock of, which the client shows beside the progress.</summary>
    [Column("snapshot_at")]
    public DateTime? SnapshotAt { get; set; }

    [Column("status")]
    public int Status { get; set; }

    /// <summary>Furniture handed over for completing the collection; null for a collection with none.</summary>
    [Column("reward_product_code")]
    [MaxLength(128)]
    public string? RewardProductCode { get; set; }

    /// <summary>A second, separate prize. Habbo draws the two apart, so they are stored apart.</summary>
    [Column("bonus_product_code")]
    [MaxLength(128)]
    public string? BonusProductCode { get; set; }

    public List<NftCollectionItemEntity>? Items { get; set; }
}
