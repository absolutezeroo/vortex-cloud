using Vortex.Protocol.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class QuizResultsMessageComposerSerializer(int header)
    : AbstractSerializer<QuizResultsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, QuizResultsMessageComposer message)
    {
        // The zero-length case is the pass screen, so a perfect score still writes its count.
        packet.WriteString(message.QuizCode).WriteInteger(message.WrongQuestionIds.Length);

        foreach (int questionId in message.WrongQuestionIds)
        {
            packet.WriteInteger(questionId);
        }
    }
}
