using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Navigator;

internal class ForwardToSomeRoomMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ForwardToSomeRoomMessage { ForwardData = packet.PopString() };
}
