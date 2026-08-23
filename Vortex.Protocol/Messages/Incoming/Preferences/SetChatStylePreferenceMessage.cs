using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Preferences;

public record SetChatStylePreferenceMessage : IMessageEvent
{
    // HabboFreeFlowChat::set preferedChatStyle() sends (chatStyle, fontSizeMode) — header 2634.
    public required int ChatStyle { get; init; }
    public required int FontSizeMode { get; init; }
}
