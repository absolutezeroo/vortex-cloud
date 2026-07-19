using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Handshake;

public record ClientHelloMessage : IMessageEvent
{
    public required string Production { get; init; }

    public required string Platform { get; init; }

    public required int ClientPlatform { get; init; }

    public required int DeviceCategory { get; init; }
}
