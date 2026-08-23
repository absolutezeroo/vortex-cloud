using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Talent;

namespace Vortex.Revisions.Revision20260701.Serializers.Talent;

internal class TalentTrackLevelMessageComposerSerializer(int header)
    : AbstractSerializer<TalentTrackLevelMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, TalentTrackLevelMessageComposer message)
    {
        //
    }
}
