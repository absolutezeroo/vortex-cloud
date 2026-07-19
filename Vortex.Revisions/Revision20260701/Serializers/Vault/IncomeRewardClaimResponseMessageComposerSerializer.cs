using Vortex.Primitives.Messages.Outgoing.Vault;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Vault;

internal class IncomeRewardClaimResponseMessageComposerSerializer(int header)
    : AbstractSerializer<IncomeRewardClaimResponseMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        IncomeRewardClaimResponseMessageComposer message
    )
    {
        packet.WriteByte((byte)message.RewardCategory).WriteBoolean(message.Result);
    }
}
