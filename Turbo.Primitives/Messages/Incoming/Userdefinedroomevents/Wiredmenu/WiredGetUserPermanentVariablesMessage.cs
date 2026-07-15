using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;

public record WiredGetUserPermanentVariablesMessage : IMessageEvent
{
    public required int EntityType { get; init; }
    public required int EntityId { get; init; }
}
