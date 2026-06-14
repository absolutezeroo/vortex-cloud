using Turbo.Primitives.Messages.Incoming.Room.Engine;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Parsers.Room.Engine;

internal class PickupObjectMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        var categoryId = packet.PopInt();
        var objectId = packet.PopInt();
        var confirm = !packet.End && packet.PopBoolean();
        return new PickupObjectMessage
        {
            CategoryId = categoryId,
            ObjectId = objectId,
            Confirm = confirm,
        };
    }
}
