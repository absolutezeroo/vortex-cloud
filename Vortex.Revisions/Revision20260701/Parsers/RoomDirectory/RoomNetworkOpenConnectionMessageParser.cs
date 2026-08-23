using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Roomdirectory;

namespace Vortex.Revisions.Revision20260701.Parsers.RoomDirectory;

internal class RoomNetworkOpenConnectionMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new RoomNetworkOpenConnectionMessage();
}
