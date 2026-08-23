using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>
/// The background toner's apply button: a colour in HSL, for the furni to tint the room with.
/// </summary>
/// <remarks>
/// Its on/off button is not this packet — the widget sends a plain <c>UseFurniture</c> (3353) for
/// that, so the toner's power state rides the ordinary state toggle and only the colour comes
/// through here.
/// </remarks>
public record SetRoomBackgroundColorDataMessage : IMessageEvent
{
    public required RoomObjectId ObjectId { get; init; }
    public required int Hue { get; init; }
    public required int Saturation { get; init; }
    public required int Lightness { get; init; }
}
