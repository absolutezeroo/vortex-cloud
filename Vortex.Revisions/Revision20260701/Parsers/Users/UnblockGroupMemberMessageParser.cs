using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Users;

namespace Vortex.Revisions.Revision20260701.Parsers.Users;

internal class UnblockGroupMemberMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new UnblockGroupMemberMessage { GroupId = packet.PopInt(), UserId = packet.PopInt() };
}
