using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;

public record WiredGetAllVariableHoldersMessage : IMessageEvent
{
    public required string VariableId { get; init; }
}
