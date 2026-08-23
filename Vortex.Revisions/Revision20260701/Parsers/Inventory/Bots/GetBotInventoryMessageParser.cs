using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Inventory.Bots;

namespace Vortex.Revisions.Revision20260701.Parsers.Inventory.Bots;

internal class GetBotInventoryMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new GetBotInventoryMessage();
}
