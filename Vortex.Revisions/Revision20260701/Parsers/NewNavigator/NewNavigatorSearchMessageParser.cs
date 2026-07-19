using Vortex.Primitives.Messages.Incoming.NewNavigator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.NewNavigator;

internal class NewNavigatorSearchMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new NewNavigatorSearchMessage
        {
            SearchCodeOriginal = packet.PopString(),
            FilteringData = packet.PopString(),
        };
}
