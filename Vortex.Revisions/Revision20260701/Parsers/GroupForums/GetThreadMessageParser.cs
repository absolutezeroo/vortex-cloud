using Vortex.Primitives.Messages.Incoming.GroupForums;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.GroupForums;

internal class GetThreadMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetThreadMessage { GroupId = packet.PopInt(), ThreadId = packet.PopInt() };
}
