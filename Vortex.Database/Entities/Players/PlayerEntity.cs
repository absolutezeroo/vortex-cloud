using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Furniture;
using Vortex.Database.Entities.Messenger;
using Vortex.Database.Entities.Room;
using Vortex.Database.Entities.Security;
using Vortex.Primitives.Players.Enums;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Database.Entities.Players;

[Table("players")]
[Index(nameof(Name), IsUnique = true)]
public class PlayerEntity : TurboEntity
{
    [Column("account_id")]
    public int? PlayerAccountEntityId { get; set; }

    [ForeignKey(nameof(PlayerAccountEntityId))]
    public PlayerAccountEntity? PlayerAccount { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [Column("motto")]
    public string? Motto { get; set; }

    [Column("figure")]
    [MaxLength(100)]
    [DefaultValue("hr-115-42.hd-195-19.ch-3030-82.lg-275-1408.fa-1201.ca-1804-64")]
    public required string Figure { get; set; }

    [Column("gender")]
    [DefaultValue(AvatarGenderType.Male)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required AvatarGenderType Gender { get; set; }

    [Column("status")]
    [DefaultValue(PlayerStatusType.Offline)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required PlayerStatusType PlayerStatus { get; set; }

    [Column("perk_flags")]
    [DefaultValue(PlayerPerkFlags.None)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required PlayerPerkFlags PlayerPerks { get; set; }

    [Column("room_chat_style_id")]
    public int? RoomChatStyleId { get; set; }

    /// <summary>Denormalised total achievement score (sum of completed levels' score points),
    /// surfaced on the profile without activating the achievement grain. Kept in sync by
    /// <c>PlayerAchievementGrain</c> on progression.</summary>
    [Column("achievement_score")]
    [DefaultValue(0)]
    public int AchievementScore { get; set; }

    /// <summary>Total respect points this player has received from others.</summary>
    [Column("respect_received")]
    [DefaultValue(0)]
    public int RespectReceived { get; set; }

    /// <summary>How many respects the player has given today (reset when <see cref="RespectResetDate"/>
    /// is not today); bounds the daily give budget.</summary>
    [Column("respect_given_today")]
    [DefaultValue(0)]
    public int RespectGivenToday { get; set; }

    /// <summary>The day <see cref="RespectGivenToday"/> applies to; a different day resets the budget.</summary>
    [Column("respect_reset_date")]
    public DateTime? RespectResetDate { get; set; }

    // The player's favourite guild, surfaced on the guild badge in the UI. Plain scalar (no FK
    // navigation) to avoid yet another circular relationship through groups/rooms.
    [Column("favourite_group_id")]
    public int? FavouriteGroupId { get; set; }

    /// <summary>Null = not locked. Player-level (not account-level): trading is a gameplay
    /// restriction on this profile, not a login/identity concern. Checked by the (not yet built)
    /// trading system.</summary>
    [Column("trading_locked_until")]
    public DateTime? TradingLockedUntil { get; set; }

    [InverseProperty("PlayerEntity")]
    public List<PlayerBadgeEntity>? PlayerBadges { get; set; }

    [InverseProperty("PlayerEntity")]
    public List<PlayerCurrencyEntity>? PlayerCurrencies { get; set; }

    [InverseProperty("PlayerEntity")]
    public List<PlayerChatStyleOwnedEntity>? PlayerOwnedChatStyles { get; set; }

    [InverseProperty("PlayerEntity")]
    public List<FurnitureEntity>? Furniture { get; set; }

    [InverseProperty("PlayerEntity")]
    public List<MessengerCategoryEntity>? MessengerCategories { get; set; }

    [InverseProperty("PlayerEntity")]
    public List<MessengerFriendEntity>? MessengerFriends { get; set; }

    [InverseProperty("RequestedPlayerEntity")]
    public List<MessengerRequestEntity>? MessengerRequests { get; set; }

    [InverseProperty("PlayerEntity")]
    public List<MessengerRequestEntity>? MessengerRequestsSent { get; set; }

    [InverseProperty("PlayerEntity")]
    public List<MessengerBlockedEntity>? MessengerBlocked { get; set; }

    [InverseProperty("PlayerEntity")]
    public List<MessengerIgnoredEntity>? MessengerIgnored { get; set; }

    [InverseProperty("PlayerEntity")]
    public List<SecurityTicketEntity>? SecurityTickets { get; set; }

    [InverseProperty("PlayerEntity")]
    public List<RoomEntity>? Rooms { get; set; }

    [InverseProperty("PlayerEntity")]
    public List<RoomBanEntity>? RoomBans { get; set; }

    [InverseProperty("PlayerEntity")]
    public List<RoomMuteEntity>? RoomMutes { get; set; }

    [InverseProperty("PlayerEntity")]
    public List<RoomRightEntity>? RoomRights { get; set; }

    [InverseProperty("PlayerEntity")]
    public List<RoomChatlogEntity>? RoomChatlogs { get; set; }

    [InverseProperty("PlayerEntity")]
    public List<PlayerSubscriptionEntity>? PlayerSubscriptions { get; set; }
}
