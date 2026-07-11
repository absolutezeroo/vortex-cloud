using Turbo.Primitives.Messages.Incoming.Users;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Parsers.Users;

internal class GetGuildMembersMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetGuildMembersMessage
        {
            GroupId = packet.PopInt(),
            PageIndex = packet.PopInt(),
            UserNameFilter = packet.PopString(),
            SearchType = packet.PopInt(),
        };
}
