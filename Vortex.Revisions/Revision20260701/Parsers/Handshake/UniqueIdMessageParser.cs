using Vortex.Primitives.Messages.Incoming.Handshake;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

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
