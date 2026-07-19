using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class GuideSessionErrorMessageComposerSerializer(int header)
    : AbstractSerializer<GuideSessionErrorMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuideSessionErrorMessageComposer message
    )
    {
        //
    }
}
