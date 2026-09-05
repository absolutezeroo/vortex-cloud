using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Habbicons;

namespace Vortex.Revisions.Revision20260701.Parsers.Habbicons;

internal class TriggerHabbiconMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new TriggerHabbiconMessage { HabbiconId = packet.PopInt() };
}
