using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Poll;

namespace Vortex.Revisions.Revision20260701.Parsers.Poll;

internal class PollStartMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new PollStartMessage { PollId = packet.PopInt() };
}
