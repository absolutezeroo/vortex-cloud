using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Preferences;

public record SetUIFlagsMessage : IMessageEvent
{
    // SetUIFlagsMessageComposer(flags) sends a single UI-flags bitmask — header 3653.
    public required int Flags { get; init; }
}
