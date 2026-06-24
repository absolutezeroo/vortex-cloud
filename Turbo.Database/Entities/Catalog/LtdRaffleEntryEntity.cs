using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Entities.Players;

namespace Turbo.Database.Entities.Catalog;

[Table("catalog_ltd_raffle_entries")]
[Index(nameof(SeriesEntityId))]
[Index(nameof(PlayerEntityId))]
public class LtdRaffleEntryEntity : TurboEntity
{
    [Column("series_id")]
    public required int SeriesEntityId { get; set; }

    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("batch_id")]
    [MaxLength(36)]
    public required string BatchId { get; set; }

    [Column("entered_at")]
    public required DateTime EnteredAt { get; set; }

    [Column("result")]
    [MaxLength(20)]
    [DefaultValue("pending")]
    public required string Result { get; set; }

    [Column("serial_number")]
    public int? SerialNumber { get; set; }

    [Column("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    [ForeignKey(nameof(SeriesEntityId))]
    public required LtdSeriesEntity SeriesEntity { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public required PlayerEntity PlayerEntity { get; set; }
}
