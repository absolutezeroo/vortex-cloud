using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Navigator;

internal class SetRoomSessionTagsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new SetRoomSessionTagsMessage { Tag1 = packet.PopString(), Tag2 = packet.PopString() };
}
