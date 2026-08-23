using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Nft;

namespace Vortex.Revisions.Revision20260701.Parsers.Nft;

public class GetSilverMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new GetSilverMessage();
}
