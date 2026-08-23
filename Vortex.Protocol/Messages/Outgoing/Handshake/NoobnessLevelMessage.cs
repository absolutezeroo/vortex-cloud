using Vortex.Primitives.Networking;
using Vortex.Primitives.Players.Enums;

namespace Vortex.Protocol.Messages.Outgoing.Handshake;

public sealed record NoobnessLevelMessage : IComposer
{
    public required NoobnessLevelType NoobnessLevel { get; init; }
}
