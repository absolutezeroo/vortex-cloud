using Turbo.Primitives.Messages.Incoming.Room.Engine;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Parsers.Room.Engine;

internal class PickupObjectMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int categoryId = packet.PopInt();
        int objectId = packet.PopInt();
        bool confirm = !packet.End && packet.PopBoolean();
        return new PickupObjectMessage
        {
            CategoryId = categoryId,
            ObjectId = objectId,
            Confirm = confirm,
        };
    }
}
