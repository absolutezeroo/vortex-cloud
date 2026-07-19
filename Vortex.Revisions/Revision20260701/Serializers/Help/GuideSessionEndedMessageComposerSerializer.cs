using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class GuideSessionEndedMessageComposerSerializer(int header)
    : AbstractSerializer<GuideSessionEndedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuideSessionEndedMessageComposer message
    )
    {
        //
    }
}
