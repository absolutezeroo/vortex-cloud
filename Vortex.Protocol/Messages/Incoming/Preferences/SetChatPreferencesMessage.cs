using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Preferences;

public record SetChatPreferencesMessage : IMessageEvent
{
    // HabboFreeFlowChat::sendChatPreferences() sends four fields — header 1149. The client's
    // composer (unknowns/_SafePkg_2091/_SafeCls_2255.as:14-19) pushes the free-flow flag and then
    // the three settings of its chat dialog; we used to read the flag alone and drop the rest, so
    // bubble mode, width and scroll speed were lost the moment the player set them.
    public required bool FreeFlowChatDisabled { get; init; }
    public required int ChatMode { get; init; }
    public required int ChatBubbleWidth { get; init; }
    public required int ChatScrollSpeed { get; init; }
}
