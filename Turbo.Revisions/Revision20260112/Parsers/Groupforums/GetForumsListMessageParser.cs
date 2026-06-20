using Turbo.Primitives.Messages.Incoming.GroupForums;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Parsers.GroupForums;

internal class GetForumsListMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetForumsListMessage
        {
            ListCode = packet.PopInt(),
            StartIndex = packet.PopInt(),
            Amount = packet.PopInt(),
        };
}
