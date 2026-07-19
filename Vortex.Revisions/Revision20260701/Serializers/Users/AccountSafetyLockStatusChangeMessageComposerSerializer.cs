using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class AccountSafetyLockStatusChangeMessageComposerSerializer(int header)
    : AbstractSerializer<AccountSafetyLockStatusChangeMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        AccountSafetyLockStatusChangeMessageComposer message
    )
    {
        //
    }
}
