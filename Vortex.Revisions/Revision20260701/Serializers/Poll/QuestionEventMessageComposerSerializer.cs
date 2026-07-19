using Vortex.Primitives.Messages.Outgoing.Poll;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Poll;

internal class QuestionEventMessageComposerSerializer(int header)
    : AbstractSerializer<QuestionEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, QuestionEventMessageComposer message)
    {
        //
    }
}
