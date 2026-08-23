using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredtrading;

internal class GetWiredTransactionDetailsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetWiredTransactionDetailsMessage { TransactionId = packet.PopLong() };
}
