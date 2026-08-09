using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Room;

/// <summary>
/// What a pet gets out of a hand item it is given. The ids are the client's own — the figure it
/// draws for <c>handitem7</c> is water because the client says so — but what water is worth to a
/// pet is a hotel's decision, which is why it lives here rather than in a table in the code.
/// <para>
/// Only consumables need a row. A hand item with none is still held and still passed around; a pet
/// simply will not take it, which is right for a camera or a bunch of roses.
/// </para>
/// </summary>
[Table("hand_items")]
[Index(nameof(HandItemId), IsUnique = true)]
public class HandItemEntity : VortexEntity
{
    /// <summary>The client's hand item id, as used by <c>CarryObjectMessageComposer</c>.</summary>
    [Column("hand_item_id")]
    public required int HandItemId { get; set; }

    [Column("name")]
    [MaxLength(64)]
    public required string Name { get; set; }

    [Column("nutrition")]
    public int Nutrition { get; set; }

    /// <summary>Water. Kept apart from nutrition the same way <c>pet_food</c> keeps it apart.</summary>
    [Column("thirst")]
    public int Thirst { get; set; }
}
