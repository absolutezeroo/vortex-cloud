using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Room.Engine;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Engine;

internal class PlaceBotMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int botId = packet.PopInt();
        int x = packet.PopInt();
        int y = packet.PopInt();

        return new PlaceBotMessage
        {
            BotId = botId,
            X = x,
            Y = y,
        };
    }
}
