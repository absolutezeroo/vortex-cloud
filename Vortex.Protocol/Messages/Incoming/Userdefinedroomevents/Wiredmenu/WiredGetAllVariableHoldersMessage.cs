using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;

public record WiredGetAllVariableHoldersMessage : IMessageEvent
{
    public required string VariableId { get; init; }
}
