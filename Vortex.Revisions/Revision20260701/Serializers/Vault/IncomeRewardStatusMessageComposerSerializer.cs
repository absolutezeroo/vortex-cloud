using Vortex.Primitives.Messages.Outgoing.Vault;
using Vortex.Primitives.Orleans.Snapshots.Vault;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Vault;

internal class IncomeRewardStatusMessageComposerSerializer(int header)
    : AbstractSerializer<IncomeRewardStatusMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        IncomeRewardStatusMessageComposer message
    )
    {
        packet.WriteInteger(message.IncomeRewards.Count);

        foreach (IncomeRewardSnapshot reward in message.IncomeRewards)
        {
            packet
                .WriteByte((byte)reward.RewardCategory)
                .WriteByte((byte)reward.RewardType)
                .WriteInteger(reward.Amount)
                .WriteString(reward.ProductCode);
        }
    }
}
