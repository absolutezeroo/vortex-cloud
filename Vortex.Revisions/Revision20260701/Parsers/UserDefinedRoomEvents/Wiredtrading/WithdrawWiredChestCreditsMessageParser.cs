using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredtrading;

internal class WithdrawWiredChestCreditsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new WithdrawWiredChestCreditsMessage()
        {
            ChestId = packet.PopInt(),
            Amount = packet.PopInt(),
        };
}
