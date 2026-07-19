using Vortex.Primitives.Messages.Outgoing.Camera;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Camera;

internal class CompetitionStatusMessageComposerSerializer(int header)
    : AbstractSerializer<CompetitionStatusMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CompetitionStatusMessageComposer message
    )
    {
        //
    }
}
