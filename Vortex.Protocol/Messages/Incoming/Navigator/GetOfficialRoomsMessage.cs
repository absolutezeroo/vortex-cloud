using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Navigator;

public record GetOfficialRoomsMessage : IMessageEvent
{
    public int AdIndex { get; init; }
}
