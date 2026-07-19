using Vortex.Primitives.Messages.Outgoing.Competition;
using Vortex.Primitives.Packets;

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
