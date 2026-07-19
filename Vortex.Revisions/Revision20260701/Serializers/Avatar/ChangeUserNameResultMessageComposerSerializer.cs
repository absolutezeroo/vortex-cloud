using Vortex.Primitives.Messages.Outgoing.Avatar;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Avatar;

internal class ChangeUserNameResultMessageComposerSerializer(int header)
    : AbstractSerializer<ChangeUserNameResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ChangeUserNameResultMessageComposer message
    )
    {
        //
    }
}
