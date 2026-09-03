using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Room.Engine;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Engine;

internal class SetItemDataMessageParser : IParser
{
    // getMessageArray() on the client's composer is [objectId, colorHex, text], and the caller is
    // FurnitureStickieWidgetHandler -> modifyRoomObjectData(objectId, 20, colorHex, text). The
    // constructor assigns its parameters out of order, so reading it instead of the array is how
    // the colour and the text end up swapped.
    public IMessageEvent Parse(IClientPacket packet) =>
        new SetItemDataMessage
        {
            ItemId = packet.PopInt(),
            ColorHex = packet.PopString(),
            Text = packet.PopString(),
        };
}
