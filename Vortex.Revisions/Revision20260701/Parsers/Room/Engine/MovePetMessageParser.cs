using Vortex.Primitives.Messages.Incoming.Room.Engine;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Engine;

internal class MovePetMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new MovePetMessage
        {
            PetId = packet.PopInt(),
            X = packet.PopInt(),
            Y = packet.PopInt(),
            Rotation = (Rotation)packet.PopInt(),
        };
}
