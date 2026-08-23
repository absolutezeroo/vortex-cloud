using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Room.Action;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Action;

internal class BanUserWithDurationMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new BanUserWithDurationMessage
        {
            UserId = packet.PopInt(),
            RoomId = packet.PopInt(),
            BanType = packet.PopString(),
        };
}
