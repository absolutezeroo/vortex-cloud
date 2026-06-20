using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Entities.Players;

namespace Turbo.Database.Entities.Security;

[Table("security_tickets")]
[Index(nameof(PlayerEntityId), IsUnique = true)]
[Index(nameof(Ticket), IsUnique = true)]
public class SecurityTicketEntity : TurboEntity
{
    [Column("player_id")]
    public int PlayerEntityId { get; set; }

    [Column("ticket")]
    public required string Ticket { get; set; }

    [Column("ip_address")]
    public required string IpAddress { get; set; }

    [Column("is_locked")]
    [DefaultValue(false)]
    public bool IsLocked { get; set; }

    /// <summary>
    /// UTC timestamp after which this ticket is no longer valid. Null means no expiration
    /// (e.g. tickets issued before this column existed, or locked/persistent tickets).
    /// </summary>
    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public required PlayerEntity PlayerEntity { get; set; }
}
