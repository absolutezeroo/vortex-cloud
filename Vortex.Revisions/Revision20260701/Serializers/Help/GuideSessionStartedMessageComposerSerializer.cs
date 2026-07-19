using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class GuideSessionStartedMessageComposerSerializer(int header)
    : AbstractSerializer<GuideSessionStartedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuideSessionStartedMessageComposer message
    )
    {
        //
    }
}
