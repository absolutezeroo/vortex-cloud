using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Userdefinedroomevents;

public record OpenMessage : IMessageEvent
{
    public required int Id { get; init; }
}
