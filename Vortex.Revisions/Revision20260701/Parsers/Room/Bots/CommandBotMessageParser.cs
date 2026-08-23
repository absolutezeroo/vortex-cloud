using Vortex.Protocol.Messages.Incoming.Room.Bots;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Bots;

internal class CommandBotMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int botId = packet.PopInt();
        int commandId = packet.PopInt();
        string data = packet.PopString();

        return new CommandBotMessage
        {
            BotId = botId,
            CommandId = commandId,
            Data = data,
        };
    }
}
