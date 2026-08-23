using Vortex.Primitives.Networking;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Collectibles;

namespace Vortex.Revisions.Revision20260701.Parsers.Collectibles;

internal class PurchaseMintTokenMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new PurchaseMintTokenMessage { OfferId = packet.PopInt(), Wallet = packet.PopString() };
}
