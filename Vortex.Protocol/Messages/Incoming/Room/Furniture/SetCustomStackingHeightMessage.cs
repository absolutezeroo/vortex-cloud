using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Messages.Incoming.Room.Furniture;

/// <summary>
/// The custom stack-height widget. The trailing flag is genuinely optional on the wire — the widget
/// sends two values when only the height changed and three when the multi-walk checkbox is what
/// moved, so a parser that always reads three desynchronises on the common case.
/// </summary>
public record SetCustomStackingHeightMessage : IMessageEvent
{
    public required RoomObjectId ObjectId { get; init; }

    /// <summary>Height in hundredths of a tile (the widget sends its float times 100). -100 is the
    /// widget's "clear the custom height" sentinel.</summary>
    public required int Height { get; init; }

    /// <summary>Whether avatars may walk over the item at every stacked level. Absent from the
    /// packet unless the checkbox is what changed.</summary>
    public bool? MultiWalkMode { get; init; }
}
