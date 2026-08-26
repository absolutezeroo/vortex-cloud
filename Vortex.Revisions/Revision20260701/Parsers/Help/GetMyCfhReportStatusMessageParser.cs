using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Help;

namespace Vortex.Revisions.Revision20260701.Parsers.Help;

internal class GetMyCfhReportStatusMessageParser : IParser
{
    // Nothing to read: WIN63's _SafeCls_2121 takes no arguments and writes no body.
    public IMessageEvent Parse(IClientPacket packet) => new GetMyCfhReportStatusMessage();
}
