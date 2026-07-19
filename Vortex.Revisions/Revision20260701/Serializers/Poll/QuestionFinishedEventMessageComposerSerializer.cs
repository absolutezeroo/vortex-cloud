using Vortex.Primitives.Messages.Outgoing.Poll;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Poll;

internal class QuestionFinishedEventMessageComposerSerializer(int header)
    : AbstractSerializer<QuestionFinishedEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        QuestionFinishedEventMessageComposer message
    )
    {
        //
    }
}
