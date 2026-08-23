using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Handshake;

namespace Vortex.Revisions.Revision20260701.Serializers.Handshake;

internal class IdentityAccountsEventMessageComposerSerializer(int header)
    : AbstractSerializer<IdentityAccountsEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        IdentityAccountsEventMessageComposer message
    )
    {
        //
    }
}
