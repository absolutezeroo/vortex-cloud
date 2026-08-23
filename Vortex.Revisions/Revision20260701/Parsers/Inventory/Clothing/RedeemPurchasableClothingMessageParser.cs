using Vortex.Primitives.Networking;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Inventory.Clothing;

namespace Vortex.Revisions.Revision20260701.Parsers.Inventory.Clothing;

internal class RedeemPurchasableClothingMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new RedeemPurchasableClothingMessage { RoomObjectId = packet.PopInt() };
}
