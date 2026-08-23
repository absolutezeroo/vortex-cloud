using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Talent;

namespace Vortex.Revisions.Revision20260701.Serializers.Talent;

internal class TalentTrackMessageComposerSerializer(int header)
    : AbstractSerializer<TalentTrackMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, TalentTrackMessageComposer message)
    {
        //
    }
}
