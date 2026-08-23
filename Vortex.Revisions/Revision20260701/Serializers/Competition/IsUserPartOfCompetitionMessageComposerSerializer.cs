using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Competition;

namespace Vortex.Revisions.Revision20260701.Serializers.Competition;

internal class IsUserPartOfCompetitionMessageComposerSerializer(int header)
    : AbstractSerializer<IsUserPartOfCompetitionMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        IsUserPartOfCompetitionMessageComposer message
    )
    {
        //
    }
}
