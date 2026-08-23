using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Users;

namespace Vortex.Revisions.Revision20260701.Parsers.Users;

public class ScrGetUserInfoMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ScrGetUserInfoMessage { ProductName = packet.PopString() };
}
