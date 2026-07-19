using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredmenu;

internal class WiredSetPreferencesMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        bool wiredMenuButton = packet.PopBoolean();
        bool wiredInspectButton = packet.PopBoolean();
        bool playTestMode = packet.PopBoolean();

        packet.PopInt(); // reserved slot, always 0 in the client

        return new WiredSetPreferencesMessage()
        {
            WiredMenuButton = wiredMenuButton,
            WiredInspectButton = wiredInspectButton,
            PlayTestMode = playTestMode,
            WiredWhisperDisabled = packet.PopBoolean(),
            ShowAllNotifications = packet.PopBoolean(),
            UiStyle = packet.PopString(),
        };
    }
}
