using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Help;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class GuideTicketResolutionMessageComposerSerializer(int header)
    : AbstractSerializer<GuideTicketResolutionMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuideTicketResolutionMessageComposer message
    )
    {
        //
    }
}
