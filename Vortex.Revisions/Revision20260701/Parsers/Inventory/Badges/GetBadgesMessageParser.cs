using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Inventory.Badges;

namespace Vortex.Revisions.Revision20260701.Parsers.Inventory.Badges;

public class GetBadgesMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new GetBadgesMessage();
}
