using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Room.Avatar;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Avatar;

internal class ChangeMottoMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ChangeMottoMessage { Text = packet.PopString() };
}
