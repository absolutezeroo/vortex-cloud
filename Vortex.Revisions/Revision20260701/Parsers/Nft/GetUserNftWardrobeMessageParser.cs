using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Nft;

namespace Vortex.Revisions.Revision20260701.Parsers.Nft;

internal class GetUserNftWardrobeMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new GetUserNftWardrobeMessage();
}
