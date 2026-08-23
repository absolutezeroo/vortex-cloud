using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading.Contracts;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading.Contracts;

internal class WiredOpenContractMessageComposerSerializer(int header)
    : AbstractSerializer<WiredOpenContractMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredOpenContractMessageComposer message
    ) => packet.WriteInteger(message.ContractId);
}
