using Vortex.Primitives.Messages.Outgoing.Avatar;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Avatar;

internal class CheckUserNameResultMessageComposerSerializer(int header)
    : AbstractSerializer<CheckUserNameResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CheckUserNameResultMessageComposer message
    )
    {
        //
    }
}
