using Vortex.Primitives.Messages.Incoming.Camera;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Camera;

internal class RenderRoomMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new RenderRoomMessage();
}
