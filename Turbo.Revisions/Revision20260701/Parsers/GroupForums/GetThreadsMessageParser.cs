using Turbo.Primitives.Messages.Incoming.GroupForums;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Parsers.GroupForums;

internal class GetThreadsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetThreadsMessage
        {
            GroupId = packet.PopInt(),
            StartIndex = packet.PopInt(),
            Amount = packet.PopInt(),
        };
}
