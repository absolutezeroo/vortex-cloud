using Vortex.Primitives.Messages.Outgoing.Nux;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Nux;

internal class NewUserExperienceNotCompleteEventMessageComposerSerializer(int header)
    : AbstractSerializer<NewUserExperienceNotCompleteEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        NewUserExperienceNotCompleteEventMessageComposer message
    )
    {
        //
    }
}
