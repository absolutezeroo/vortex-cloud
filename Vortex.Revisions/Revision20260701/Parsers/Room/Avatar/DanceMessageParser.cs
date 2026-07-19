using Vortex.Primitives.Messages.Incoming.Room.Avatar;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Avatar;

internal class DanceMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new DanceMessage { DanceId = packet.PopInt() };
}
