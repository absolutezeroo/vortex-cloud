using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Furniture;

namespace Vortex.Database.Entities.Wired;

/// <summary>
/// A wired chest's own state: what it holds in currency, and how it behaves.
/// </summary>
/// <remarks>
/// The furniture it belongs to is the identity — a chest is a furni, and picking that furni up takes
/// its chest with it. What the chest holds in <em>furniture</em> is not here but on the furniture
/// itself (<c>furniture.wired_chest_id</c>), because a stored furni is a real inventory row that has
/// to keep its own identity, its stuff data and its history: a chest holds furniture, it does not
/// dissolve it into a count.
/// </remarks>
[Table("wired_chests")]
[Index(nameof(FurnitureEntityId), IsUnique = true)]
public class WiredChestEntity : VortexEntity
{
    [Column("furniture_id")]
    public required int FurnitureEntityId { get; set; }

    /// <summary>Credits the chest holds, deposited by whoever fills it. This is real currency parked
    /// in a furni, so it moves only through the same debit/credit path a purchase uses.</summary>
    [Column("credits")]
    [DefaultValue(0)]
    public required int Credits { get; set; }

    /// <summary>Whether the chest tells its owner when something is taken from it.</summary>
    [Column("notifications_enabled")]
    [DefaultValue(true)]
    public required bool NotificationsEnabled { get; set; }

    /// <summary>The name the owner gave the chest, shown on its screen.</summary>
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>The description under the name. The client offers a free-text box for both.</summary>
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>"Everyone can open the chest" — otherwise only whoever may decorate the room.</summary>
    [Column("everyone_can_open")]
    [DefaultValue(false)]
    public bool EveryoneCanOpen { get; set; }

    /// <summary>"Everyone can donate to the chest".</summary>
    [Column("everyone_can_donate")]
    [DefaultValue(false)]
    public bool EveryoneCanDonate { get; set; }

    /// <summary>"Chest state" — which of the furni's appearances it wears. The client picks it from a
    /// dropdown whose entries come from the furni itself, so the server stores the choice rather
    /// than judging it.</summary>
    [Column("chest_state")]
    [DefaultValue(0)]
    public int ChestState { get; set; }

    /// <summary>"Items to preview in open state".</summary>
    [Column("preview_items")]
    [DefaultValue(0)]
    public int PreviewItems { get; set; }

    /// <summary>"Amount of preview items".</summary>
    [Column("preview_amount")]
    [DefaultValue(0)]
    public int PreviewAmount { get; set; }

    /// <summary>0 = always notify, 1 = only when the owner is not in the room.</summary>
    [Column("notification_mode")]
    [DefaultValue(0)]
    public int NotificationMode { get; set; }

    /// <summary>"Notify me when the chest is full".</summary>
    [Column("notify_when_full")]
    [DefaultValue(false)]
    public bool NotifyWhenFull { get; set; }

    /// <summary>"Notify me when someone makes a donation".</summary>
    [Column("notify_on_donation")]
    [DefaultValue(false)]
    public bool NotifyOnDonation { get; set; }

    /// <summary>"Notify me when someone withdraws from chest".</summary>
    [Column("notify_on_withdraw")]
    [DefaultValue(false)]
    public bool NotifyOnWithdraw { get; set; }

    /// <summary>"Notify me when the chest is empty".</summary>
    [Column("notify_when_empty")]
    [DefaultValue(false)]
    public bool NotifyWhenEmpty { get; set; }

    /// <summary>"Notify me for any Wired transaction".</summary>
    [Column("notify_on_any_wired_transaction")]
    [DefaultValue(false)]
    public bool NotifyOnAnyWiredTransaction { get; set; }

    /// <summary>Locked chests refuse withdrawals.</summary>
    [Column("locked")]
    [DefaultValue(false)]
    public bool Locked { get; set; }

    /// <summary>Whether the chest locks itself again after being used.</summary>
    [Column("auto_lock")]
    [DefaultValue(false)]
    public bool AutoLock { get; set; }

    /// <summary>How many items the chest may hold. Bought, not chosen: the client sends a number
    /// alongside the lock flags and the server does not take it, because a capacity a client can
    /// name is a capacity anyone can set to a million. It moves through the upgrade purchase.</summary>
    [Column("capacity")]
    [DefaultValue(0)]
    public int Capacity { get; set; }

    [ForeignKey(nameof(FurnitureEntityId))]
    public FurnitureEntity? Furniture { get; set; }

    public IList<FurnitureEntity>? StoredFurniture { get; set; }
}
