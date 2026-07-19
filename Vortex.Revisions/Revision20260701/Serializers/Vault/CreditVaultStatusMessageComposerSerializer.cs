using Vortex.Primitives.Messages.Outgoing.Vault;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Vault;

internal class CreditVaultStatusMessageComposerSerializer(int header)
    : AbstractSerializer<CreditVaultStatusMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CreditVaultStatusMessageComposer message
    )
    {
        packet
            .WriteBoolean(message.IsUnlocked)
            .WriteInteger(message.TotalBalance)
            .WriteInteger(message.WithdrawBalance);
    }
}
