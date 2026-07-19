using Vortex.Primitives.Messages.Outgoing.Userclassification;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.UserClassification;

internal class UserClassificationMessageComposerSerializer(int header)
    : AbstractSerializer<UserClassificationMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        UserClassificationMessageComposer message
    )
    {
        //
    }
}
