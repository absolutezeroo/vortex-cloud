using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredtrading;

internal class UpgradeWiredChestMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new UpgradeWiredChestMessage { ChestId = packet.PopInt(), UpgradeType = packet.PopInt() };
}
