using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Navigator;

public record GetOfficialRoomsMessage : IMessageEvent
{
    public int AdIndex { get; init; }
}
