using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Register;

public record UpdateFigureDataMessage : IMessageEvent
{
    public required string Figure { get; init; }
    public required string Gender { get; init; }
}
