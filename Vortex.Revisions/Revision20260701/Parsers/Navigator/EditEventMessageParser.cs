using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Navigator;

internal class EditEventMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new EditEventMessage
        {
            Id = packet.PopInt(),
            Name = packet.PopString(),
            Description = packet.PopString(),
        };
}
