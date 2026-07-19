using Vortex.Primitives.Messages.Incoming.Room.Action;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Action;

internal class RemoveAllRightsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new RemoveAllRightsMessage();
}
