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

    [ForeignKey(nameof(FurnitureEntityId))]
    public FurnitureEntity? Furniture { get; set; }

    public IList<FurnitureEntity>? StoredFurniture { get; set; }
}
