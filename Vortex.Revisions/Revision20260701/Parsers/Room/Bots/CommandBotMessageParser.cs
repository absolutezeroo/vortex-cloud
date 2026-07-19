using Vortex.Primitives.Messages.Incoming.Room.Bots;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Bots;

internal class CommandBotMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new CommandBotMessage();
}
