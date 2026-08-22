using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;
using Vortex.Database.Entities.Room;

namespace Vortex.Database.Entities.Wired;

/// <summary>
/// One movement in or out of a wired chest, kept for the log screens.
/// </summary>
/// <remarks>
/// The client reads this list two ways and says which in the answer: by chest, and by room. Both are
/// the same rows, so they are one table indexed on both sides rather than two logs to keep in step.
/// <para>
/// The player's name is stored alongside the id on purpose. A log is a historical record: it should
/// say who did it under the name they used, not under the name they have today.
/// </para>
/// </remarks>
[Table("wired_chest_transactions")]
[Index(nameof(WiredChestEntityId))]
[Index(nameof(RoomEntityId))]
public class WiredChestTransactionEntity : VortexEntity
{
    [Column("wired_chest_id")]
    public required int WiredChestEntityId { get; set; }

    /// <summary>The room the chest stood in. The client calls it the flat id and shows the room
    /// log under it.</summary>
    [Column("room_id")]
    public required int RoomEntityId { get; set; }

    /// <summary>0 manual, 1 wired, 2 contract payment, 3 contract reward, 4 contract trade,
    /// 5 auto-withdraw — the client localises the number, so these are its values, not ours. Only
    /// manual is written today: the rest need wired boxes or the contract furni, which this hotel
    /// does not have.</summary>
    [Column("transaction_type")]
    [DefaultValue(0)]
    public required int TransactionType { get; set; }

    /// <summary>What moved, in one line, for the details screen. Empty for a currency movement,
    /// which the counts already describe.</summary>
    [Column("definition_info")]
    public string DefinitionInfo { get; set; } = string.Empty;

    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("player_name")]
    public required string PlayerName { get; set; }

    /// <summary>How many chests the movement touched. One, for anything a player does by hand.</summary>
    [Column("chest_count")]
    [DefaultValue(1)]
    public required int ChestCount { get; set; }

    [Column("withdraw_furni_count")]
    [DefaultValue(0)]
    public required int WithdrawFurniCount { get; set; }

    [Column("deposit_furni_count")]
    [DefaultValue(0)]
    public required int DepositFurniCount { get; set; }

    [Column("withdraw_coins_count")]
    [DefaultValue(0)]
    public required int WithdrawCoinsCount { get; set; }

    [Column("deposit_coins_count")]
    [DefaultValue(0)]
    public required int DepositCoinsCount { get; set; }

    [ForeignKey(nameof(WiredChestEntityId))]
    public WiredChestEntity? WiredChest { get; set; }

    [ForeignKey(nameof(RoomEntityId))]
    public RoomEntity? Room { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? Player { get; set; }
}
