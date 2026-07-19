using Vortex.Primitives.Messages.Incoming.Tracking;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Tracking;

internal class LatencyPingRequestMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new LatencyPingRequestMessage { RequestId = packet.PopInt() };
}
