using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Hotlooks;

namespace Vortex.Revisions.Revision20260701.Parsers.Hotlooks;

internal class GetHotLooksMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new GetHotLooksMessage();
}
