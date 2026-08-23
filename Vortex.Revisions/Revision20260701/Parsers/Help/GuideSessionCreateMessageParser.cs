using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Help;

namespace Vortex.Revisions.Revision20260701.Parsers.Help;

internal class GuideSessionCreateMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GuideSessionCreateMessage
        {
            HelpRequestType = packet.PopInt(),
            Description = packet.PopString(),
        };
}
