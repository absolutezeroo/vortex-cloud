using Vortex.Primitives.Messages.Incoming.Room.Session;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Session;

internal class QuitMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new QuitMessage();
}
