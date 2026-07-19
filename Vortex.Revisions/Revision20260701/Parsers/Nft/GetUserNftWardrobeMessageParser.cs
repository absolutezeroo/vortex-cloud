using Vortex.Primitives.Messages.Incoming.Nft;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Nft;

internal class GetUserNftWardrobeMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new GetUserNftWardrobeMessage();
}
