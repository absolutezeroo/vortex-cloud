using Vortex.Primitives.Messages.Incoming.Inventory.Badges;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Inventory.Badges;

internal class GetIsBadgeRequestFulfilledMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetIsBadgeRequestFulfilledMessage { RequestCode = packet.PopString() };
}
