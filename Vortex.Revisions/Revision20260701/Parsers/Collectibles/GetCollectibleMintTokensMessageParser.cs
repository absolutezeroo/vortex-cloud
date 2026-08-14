using Vortex.Primitives.Messages.Incoming.Collectibles;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Collectibles;

internal class GetCollectibleMintTokensMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetCollectibleMintTokensMessage { Wallet = packet.PopString() };
}
