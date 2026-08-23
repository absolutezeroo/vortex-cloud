using Vortex.Primitives.Networking;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Collectibles;

namespace Vortex.Revisions.Revision20260701.Parsers.Collectibles;

internal class NftStorePurchaseMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new NftStorePurchaseMessage
        {
            ProductCode = packet.PopString(),
            Wallet = packet.PopString(),
        };
}
