using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Moderator;

namespace Vortex.Revisions.Revision20260701.Parsers.Moderator;

internal class GetRoomVisitsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetRoomVisitsMessage { UserId = packet.PopInt() };
}
