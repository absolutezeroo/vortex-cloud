using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Users;

internal class UnblockUserMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new UnblockUserMessage { PlayerId = packet.PopInt() };
}
