using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Users;

internal class GetMemberGuildItemCountMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetMemberGuildItemCountMessage { GroupId = packet.PopInt(), UserId = packet.PopInt() };
}
