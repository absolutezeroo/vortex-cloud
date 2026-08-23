using Vortex.Protocol.Messages.Incoming.Collectibles;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Primitives.Packets;

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
