using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Preferences;

public record SetChatPreferencesMessage : IMessageEvent
{
    // HabboFreeFlowChat::sendChatPreferences() sends the free-flow-chat-disabled flag — header 1149.
    public required bool FreeFlowChatDisabled { get; init; }
}
