using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Crafting;

namespace Vortex.Revisions.Revision20260701.Parsers.Crafting;

internal class CraftMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new CraftMessage();
}
