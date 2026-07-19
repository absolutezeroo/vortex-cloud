using Vortex.Primitives.Messages.Incoming.NewNavigator;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.NewNavigator;

internal class NavigatorSetSearchCodeViewModeMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new NavigatorSetSearchCodeViewModeMessage
        {
            CategoryName = packet.PopString(),
            ViewMode = (NavigatorViewModeType)packet.PopInt(),
        };
}
