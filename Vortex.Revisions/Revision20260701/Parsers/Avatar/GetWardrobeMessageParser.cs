using Vortex.Primitives.Messages.Incoming.Avatar;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Avatar;

internal class GetWardrobeMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new GetWardrobeMessage();
}
