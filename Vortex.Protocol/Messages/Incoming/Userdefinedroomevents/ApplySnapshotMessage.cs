using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Userdefinedroomevents;

public record ApplySnapshotMessage : IMessageEvent
{
    public required int Id { get; init; }
}
