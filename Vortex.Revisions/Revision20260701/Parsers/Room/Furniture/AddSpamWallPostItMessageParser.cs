using Vortex.Primitives.Messages.Incoming.Room.Furniture;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Furniture;

internal class AddSpamWallPostItMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        // The composer reorders its own constructor arguments: it takes (objectId, location, colour,
        // text) but emits location, then COLOUR, then text.
        int objectId = packet.PopInt();
        string location = packet.PopString();
        string colorHex = packet.PopString();
        string text = packet.PopString();

        return new AddSpamWallPostItMessage
        {
            ObjectId = new RoomObjectId(objectId),
            Location = location,
            ColorHex = colorHex,
            Text = text,
        };
    }
}
