using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Furniture;

namespace Vortex.Database.Entities.Wired;

/// <summary>
/// What a wired trading contract asks for, as its owner wrote it.
/// </summary>
/// <remarks>
/// The furniture is the identity, the way a chest's is: a contract is a furni, and picking it up
/// takes its terms with it.
/// <para>
/// <see cref="Definition"/> is the terms themselves, and it is stored as JSON rather than as
/// columns because it is a tree — alternatives, each a bundle of terms, each naming a kind of
/// furniture or an amount of coins. Flattening that into rows would buy nothing: nothing queries
/// inside it, and the one reader wants the whole thing at once.
/// </para>
/// </remarks>
[Table("wired_contracts")]
[Index(nameof(FurnitureEntityId), IsUnique = true)]
public class WiredContractEntity : VortexEntity
{
    [Column("furniture_id")]
    public required int FurnitureEntityId { get; set; }

    /// <summary>0 payment, 1 trade, 2 reward — the client's own three, and it decides the tail.</summary>
    [Column("contract_type")]
    [DefaultValue(0)]
    public required int ContractType { get; set; }

    /// <summary>The rules, as JSON. Empty until the owner has saved the contract once.</summary>
    [Column("definition")]
    public string Definition { get; set; } = string.Empty;

    /// <summary>Payment contracts only.</summary>
    [Column("payment_mode")]
    [DefaultValue(0)]
    public int PaymentMode { get; set; }

    /// <summary>What the screen tells the player they get. Payment contracts only.</summary>
    [Column("receive_text")]
    public string ReceiveText { get; set; } = string.Empty;

    /// <summary>Which of the trade screen's layouts to draw. Payment contracts only.</summary>
    [Column("layout_type")]
    public string LayoutType { get; set; } = string.Empty;

    /// <summary>Reward contracts only: which earnings category the reward is paid into.</summary>
    [Column("reward_category")]
    [DefaultValue(0)]
    public int RewardCategory { get; set; }

    /// <summary>Reward contracts only: whether the reward pop-up shows without being asked for.</summary>
    [Column("show_dialog")]
    [DefaultValue(false)]
    public bool ShowDialog { get; set; }

    /// <summary>Reward contracts only: the text on that pop-up.</summary>
    [Column("reward_text")]
    public string RewardText { get; set; } = string.Empty;

    [ForeignKey(nameof(FurnitureEntityId))]
    public FurnitureEntity? Furniture { get; set; }
}
