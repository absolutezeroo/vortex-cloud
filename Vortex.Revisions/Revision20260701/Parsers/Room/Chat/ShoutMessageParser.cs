using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Room.Chat;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Chat;

internal class ShoutMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ShoutMessage { Text = packet.PopString(), StyleId = packet.PopInt() };
}
