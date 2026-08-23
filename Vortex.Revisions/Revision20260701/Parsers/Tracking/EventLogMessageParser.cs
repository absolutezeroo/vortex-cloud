using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Tracking;

namespace Vortex.Revisions.Revision20260701.Parsers.Tracking;

internal class EventLogMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new EventLogMessage
        {
            Event = packet.PopString(),
            Data = packet.PopString(),
            Action = packet.PopString(),
            ExtraString = packet.PopString(),
            ExtraInt = packet.PopInt(),
        };
}
