using Vortex.Primitives.Messages.Incoming.NewNavigator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.NewNavigator;

internal class NavigatorAddSavedSearchMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new NavigatorAddSavedSearchMessage
        {
            SearchCode = packet.PopString(),
            Filter = packet.PopString(),
        };
}
