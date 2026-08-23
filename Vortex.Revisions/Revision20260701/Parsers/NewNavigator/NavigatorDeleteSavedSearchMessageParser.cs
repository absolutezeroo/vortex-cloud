using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.NewNavigator;

namespace Vortex.Revisions.Revision20260701.Parsers.NewNavigator;

internal class NavigatorDeleteSavedSearchMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new NavigatorDeleteSavedSearchMessage { SearchId = packet.PopInt() };
}
