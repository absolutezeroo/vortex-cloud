using Vortex.Primitives.Messages.Incoming.Inventory;
using Vortex.Primitives.Messages.Incoming.Inventory.Purse;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Inventory.Purse;

internal class GetCreditsInfoMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new GetCreditsInfoMessage();
}
