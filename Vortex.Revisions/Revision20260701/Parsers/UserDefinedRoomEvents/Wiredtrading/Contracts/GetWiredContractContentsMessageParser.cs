using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading.Contracts;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredtrading.Contracts;

internal class GetWiredContractContentsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetWiredContractContentsMessage { ContractId = packet.PopInt() };
}
