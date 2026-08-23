using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Help;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class GuideTicketCreationResultMessageComposerSerializer(int header)
    : AbstractSerializer<GuideTicketCreationResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuideTicketCreationResultMessageComposer message
    )
    {
        //
    }
}
