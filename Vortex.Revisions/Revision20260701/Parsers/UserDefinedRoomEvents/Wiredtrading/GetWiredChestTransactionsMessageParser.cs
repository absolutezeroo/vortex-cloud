using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredtrading;

internal class GetWiredChestTransactionsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetWiredChestTransactionsMessage
        {
            ChestId = packet.PopInt(),
            PageSize = packet.PopInt(),
            Page = packet.PopInt(),
        };
}
