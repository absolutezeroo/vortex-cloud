using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Users;

public class ScrGetUserInfoMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ScrGetUserInfoMessage { ProductName = packet.PopString() };
}
