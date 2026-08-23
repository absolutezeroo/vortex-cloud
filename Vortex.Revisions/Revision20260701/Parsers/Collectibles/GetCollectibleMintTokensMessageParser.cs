using Vortex.Primitives.Networking;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Collectibles;

namespace Vortex.Revisions.Revision20260701.Parsers.Collectibles;

internal class GetCollectibleMintTokensMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetCollectibleMintTokensMessage { Wallet = packet.PopString() };
}
