using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Competition;

namespace Vortex.Revisions.Revision20260701.Serializers.Competition;

internal class CompetitionVotingInfoMessageComposerSerializer(int header)
    : AbstractSerializer<CompetitionVotingInfoMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CompetitionVotingInfoMessageComposer message
    )
    {
        //
    }
}
