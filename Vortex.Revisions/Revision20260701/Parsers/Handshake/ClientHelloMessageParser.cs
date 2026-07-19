using Vortex.Primitives.Messages.Incoming.Handshake;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Handshake;

internal class ClientHelloMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ClientHelloMessage
        {
            Production = packet.PopString(),
            Platform = packet.PopString(),
            ClientPlatform = packet.PopInt(),
            DeviceCategory = packet.PopInt(),
        };
}
