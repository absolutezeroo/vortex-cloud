using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Players;

/// <summary>
/// One row per account, upserted (mirrors <c>RoomBanEntity</c>/<c>RoomMuteEntity</c>): re-banning
/// reuses the existing row (new <see cref="DateExpires"/>, <c>DeletedAt</c> reset to null) instead of
/// inserting a duplicate, and unbanning soft-deletes it rather than nulling a column.
/// </summary>
[Table("account_bans")]
[Index(nameof(PlayerAccountEntityId), IsUnique = true)]
public class AccountBanEntity : VortexEntity
{
    [Column("account_id")]
    public required int PlayerAccountEntityId { get; set; }

    [Column("date_expires")]
    public required DateTime DateExpires { get; set; }

    [Column("reason")]
    [MaxLength(255)]
    public string? Reason { get; set; }

    [ForeignKey(nameof(PlayerAccountEntityId))]
    public required PlayerAccountEntity PlayerAccountEntity { get; set; }
}
