using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;

public record WiredSetObjectVariableValueMessage : IMessageEvent
{
    public required int EntityType { get; init; }
    public required int EntityId { get; init; }
    public required string VariableId { get; init; }
    public required int Value { get; init; }
    public required int Action { get; init; }
}
