using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class ChangeEmailResultEventMessageComposerSerializer(int header)
    : AbstractSerializer<ChangeEmailResultEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ChangeEmailResultEventMessageComposer message
    )
    {
        //
    }
}
