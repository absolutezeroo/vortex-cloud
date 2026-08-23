using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Room.Session;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Session;

internal class ChangeQueueMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new ChangeQueueMessage();
}
