using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading.Contracts;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading.Contracts;

internal class WiredContractUpdateResultMessageComposerSerializer(int header)
    : AbstractSerializer<WiredContractUpdateResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredContractUpdateResultMessageComposer message
    ) =>
        packet
            .WriteInteger(message.ContractId)
            .WriteBoolean(message.IsSuccess)
            .WriteString(message.FailCode);
}
