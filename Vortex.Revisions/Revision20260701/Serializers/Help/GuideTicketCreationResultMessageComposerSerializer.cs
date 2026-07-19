using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

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
