using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredmenu;

public record WiredGetVariablesForObjectMessage : IMessageEvent
{
    public required int SourceType { get; init; }
    public required int SourceId { get; init; }
}
