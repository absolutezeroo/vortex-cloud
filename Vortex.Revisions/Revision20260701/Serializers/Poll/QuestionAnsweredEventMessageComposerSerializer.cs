using Vortex.Primitives.Messages.Outgoing.Poll;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Poll;

internal class QuestionAnsweredEventMessageComposerSerializer(int header)
    : AbstractSerializer<QuestionAnsweredEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        QuestionAnsweredEventMessageComposer message
    )
    {
        //
    }
}
