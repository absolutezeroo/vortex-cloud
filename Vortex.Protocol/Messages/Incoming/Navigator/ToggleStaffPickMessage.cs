using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;

namespace Vortex.Primitives.Messages.Incoming.Navigator;

public record ToggleStaffPickMessage : IMessageEvent
{
    public RoomId RoomId { get; init; }
    public bool IsStaffPicked { get; init; }
}
