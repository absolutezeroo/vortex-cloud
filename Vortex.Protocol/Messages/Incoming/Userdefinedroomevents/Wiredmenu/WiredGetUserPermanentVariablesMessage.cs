using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredmenu;

public record WiredGetUserPermanentVariablesMessage : IMessageEvent
{
    public required int EntityType { get; init; }
    public required int EntityId { get; init; }
}
