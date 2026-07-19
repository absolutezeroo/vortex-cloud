using Vortex.Primitives.Messages.Incoming.Roomdirectory;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.RoomDirectory;

internal class RoomNetworkOpenConnectionMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new RoomNetworkOpenConnectionMessage();
}
