using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredtrading;

internal class SetAllWiredChestLocksMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new SetAllWiredChestLocksMessage
        {
            Locked = packet.PopBoolean(),
            ApplyToAll = packet.PopBoolean(),
        };
}
