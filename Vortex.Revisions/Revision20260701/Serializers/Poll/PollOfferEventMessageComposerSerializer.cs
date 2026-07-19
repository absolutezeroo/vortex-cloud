using Vortex.Primitives.Messages.Outgoing.Poll;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Poll;

internal class PollOfferEventMessageComposerSerializer(int header)
    : AbstractSerializer<PollOfferEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, PollOfferEventMessageComposer message)
    {
        //
    }
}
