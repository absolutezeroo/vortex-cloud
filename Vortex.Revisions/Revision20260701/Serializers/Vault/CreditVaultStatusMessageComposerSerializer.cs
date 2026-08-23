using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Vault;

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
