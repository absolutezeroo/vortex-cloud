using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class GuideSessionAttachedMessageComposerSerializer(int header)
    : AbstractSerializer<GuideSessionAttachedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuideSessionAttachedMessageComposer message
    )
    {
        //
    }
}
