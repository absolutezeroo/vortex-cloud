using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Tracking;

namespace Vortex.Revisions.Revision20260701.Parsers.Tracking;

internal class LagWarningReportMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new LagWarningReportMessage { WarningCount = packet.PopInt() };
}
