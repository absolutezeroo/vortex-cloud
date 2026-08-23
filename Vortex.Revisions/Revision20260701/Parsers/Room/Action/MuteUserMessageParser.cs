using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Room.Action;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Action;

internal class MuteUserMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new MuteUserMessage
        {
            UserId = packet.PopInt(),
            Minutes = packet.PopInt(),
            RoomId = packet.PopInt(),
        };
}
