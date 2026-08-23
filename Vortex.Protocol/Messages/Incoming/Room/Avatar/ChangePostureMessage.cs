using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Primitives.Messages.Incoming.Room.Avatar;

public record ChangePostureMessage : IMessageEvent
{
    public AvatarPostureType PostureType { get; init; }
}
