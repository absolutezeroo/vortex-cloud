using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredmenu;

public record WiredGetAllVariableHoldersMessage : IMessageEvent
{
    public required string VariableId { get; init; }
}
