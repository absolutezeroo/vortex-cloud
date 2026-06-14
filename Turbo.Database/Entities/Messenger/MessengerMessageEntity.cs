using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Entities.Players;

namespace Turbo.Database.Entities.Messenger;

[Table("messenger_messages")]
[Index(nameof(ReceiverEntityId), nameof(SenderEntityId), nameof(Timestamp))]
[Index(nameof(SenderEntityId), nameof(ReceiverEntityId), nameof(Timestamp))]
public class MessengerMessageEntity : TurboEntity
{
    [Column("sender_id")]
    public required int SenderEntityId { get; set; }

    [Column("receiver_id")]
    public required int ReceiverEntityId { get; set; }

    [Column("message")]
    [MaxLength(512)]
    public required string Message { get; set; }

    [Column("timestamp")]
    public required DateTime Timestamp { get; set; }

    [Column("delivered")]
    [DefaultValue(false)]
    public required bool Delivered { get; set; }

    [ForeignKey(nameof(SenderEntityId))]
    public required PlayerEntity SenderEntity { get; set; }

    [ForeignKey(nameof(ReceiverEntityId))]
    public required PlayerEntity ReceiverEntity { get; set; }
}
