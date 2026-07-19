using Vortex.Primitives.Messages.Outgoing.Competition;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Competition;

internal class CompetitionEntrySubmitResultMessageComposerSerializer(int header)
    : AbstractSerializer<CompetitionEntrySubmitResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CompetitionEntrySubmitResultMessageComposer message
    )
    {
        //
    }
}
