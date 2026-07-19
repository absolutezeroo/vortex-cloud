using Vortex.Primitives.Messages.Incoming.Room.Engine;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Engine;

internal class PlaceBotMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new PlaceBotMessage();
}
