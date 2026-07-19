using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Users;

internal class GetHabboGroupDetailsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetHabboGroupDetailsMessage
        {
            GroupId = packet.PopInt(),
            RequestDetails = packet.PopBoolean(),
        };
}
