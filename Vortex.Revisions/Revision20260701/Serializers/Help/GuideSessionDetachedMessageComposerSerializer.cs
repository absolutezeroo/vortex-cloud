using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class GuideSessionDetachedMessageComposerSerializer(int header)
    : AbstractSerializer<GuideSessionDetachedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuideSessionDetachedMessageComposer message
    )
    {
        //
    }
}
