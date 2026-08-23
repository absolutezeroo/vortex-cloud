using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Preferences;

namespace Vortex.Revisions.Revision20260701.Parsers.Preferences;

internal class SetNewNavigatorWindowPreferencesMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new SetNewNavigatorWindowPreferencesMessage
        {
            X = packet.PopInt(),
            Y = packet.PopInt(),
            Width = packet.PopInt(),
            Height = packet.PopInt(),
            OpenSavedSearches = packet.PopBoolean(),
            ResultsMode = (NavigatorViewModeType)packet.PopInt(),
        };
}
