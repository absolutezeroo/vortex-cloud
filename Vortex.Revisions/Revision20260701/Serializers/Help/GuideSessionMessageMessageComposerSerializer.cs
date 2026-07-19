using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class GuideSessionMessageMessageComposerSerializer(int header)
    : AbstractSerializer<GuideSessionMessageMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuideSessionMessageMessageComposer message
    )
    {
        //
    }
}
