using Vortex.Primitives.Messages.Incoming.Nux;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Nux;

internal class SelectInitialRoomMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new SelectInitialRoomMessage();
}
