using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Room.Avatar;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Avatar;

internal class DanceMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new DanceMessage { DanceId = packet.PopInt() };
}
