using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredtrading;

internal class SetWiredChestLockMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new SetWiredChestLockMessage
        {
            ChestId = packet.PopInt(),
            Locked = packet.PopBoolean(),
            AutoLock = packet.PopBoolean(),
            RequestedCapacity = packet.PopInt(),
        };
}
