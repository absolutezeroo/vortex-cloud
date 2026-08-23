using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Handshake;

public record VersionCheckMessage : IMessageEvent
{
    public required int ClientID { get; init; }
    public required string ClientURL { get; init; }
    public required string ExternalVariablesURL { get; init; }
}
