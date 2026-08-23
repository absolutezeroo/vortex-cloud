using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Handshake;

namespace Vortex.Revisions.Revision20260701.Parsers.Handshake;

internal class UniqueIdMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new UniqueIdMessage
        {
            MachineID = packet.PopString(),
            Fingerprint = packet.PopString(),
            FlashVersion = packet.PopString(),
        };
}
