using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class QuizResultsMessageComposerSerializer(int header)
    : AbstractSerializer<QuizResultsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, QuizResultsMessageComposer message)
    {
        //
    }
}
