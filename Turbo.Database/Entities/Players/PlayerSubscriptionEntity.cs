using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Turbo.Primitives.Players.Enums;

namespace Turbo.Database.Entities.Players;

[Table("player_subscriptions")]
[Index(nameof(PlayerEntityId), nameof(SubscriptionType), IsUnique = true)]
public class PlayerSubscriptionEntity : TurboEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("subscription_type")]
    public required SubscriptionType SubscriptionType { get; set; }

    [Column("level")]
    public int Level { get; set; } = 1;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("total_months")]
    public int TotalMonths { get; set; } = 0;

    [Column("gifts_available")]
    public int GiftsAvailable { get; set; } = 0;

    [Column("next_gift_at")]
    public DateTime? NextGiftAt { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public required PlayerEntity PlayerEntity { get; set; }
}
