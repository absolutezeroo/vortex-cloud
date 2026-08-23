using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Moderator;

public record ModAlertMessage : IMessageEvent
{
    public required int UserId { get; init; }
    public required string Message { get; init; }
    public int Topic { get; init; }
}
