using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class GuideOnDutyStatusMessageComposerSerializer(int header)
    : AbstractSerializer<GuideOnDutyStatusMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuideOnDutyStatusMessageComposer message
    )
    {
        //
    }
}
