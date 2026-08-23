using Vortex.Protocol.Messages.Incoming.Room.Furniture;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Furniture;

internal class SetCustomStackingHeightMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int objectId = packet.PopInt();
        int height = packet.PopInt();

        // The widget builds its payload as an array and only appends the multi-walk flag when the
        // checkbox is what changed, so the third value is present or absent, never defaulted.
        bool? multiWalkMode = packet.End ? null : packet.PopBoolean();

        return new SetCustomStackingHeightMessage
        {
            ObjectId = new RoomObjectId(objectId),
            Height = height,
            MultiWalkMode = multiWalkMode,
        };
    }
}
