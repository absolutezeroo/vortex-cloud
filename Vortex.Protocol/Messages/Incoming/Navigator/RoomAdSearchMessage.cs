using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Navigator;

public record RoomAdSearchMessage : IMessageEvent
{
    public int AdIndex { get; init; }
    public int TabId { get; init; }
}
