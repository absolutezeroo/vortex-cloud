using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Habbicons;

namespace Vortex.Revisions.Revision20260701.Parsers.Habbicons;

internal class BuyHabbiconCollectionMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new BuyHabbiconCollectionMessage { CollectionId = packet.PopInt() };
}
