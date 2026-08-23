using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Inventory.Trading;

namespace Vortex.Revisions.Revision20260701.Parsers.Inventory.Trading;

internal class CloseTradingMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new CloseTradingMessage();
}
