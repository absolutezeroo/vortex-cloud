using Vortex.Primitives.Messages.Incoming.Hotlooks;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Hotlooks;

internal class GetHotLooksMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new GetHotLooksMessage();
}
