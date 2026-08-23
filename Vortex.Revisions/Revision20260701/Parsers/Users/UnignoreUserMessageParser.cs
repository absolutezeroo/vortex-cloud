using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Users;

namespace Vortex.Revisions.Revision20260701.Parsers.Users;

internal class UnignoreUserMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new UnignoreUserMessage { UserId = packet.PopInt() };
}
