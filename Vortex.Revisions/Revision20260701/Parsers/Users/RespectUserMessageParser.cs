using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Users;

namespace Vortex.Revisions.Revision20260701.Parsers.Users;

internal class RespectUserMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new RespectUserMessage { UserId = packet.PopInt() };
}
