using Vortex.Primitives.Messages.Outgoing.Poll;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Poll;

/// <summary>
/// Opens the live word-quiz question: poll type, poll id, question id, duration, then the question
/// block itself in the same layout the survey uses.
/// </summary>
internal class QuestionEventMessageComposerSerializer(int header)
    : AbstractSerializer<QuestionEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, QuestionEventMessageComposer message)
    {
        packet.WriteString(message.PollType);
        packet.WriteInteger(message.PollId);
        packet.WriteInteger(message.QuestionId);
        packet.WriteInteger(message.Duration);

        PollQuestionWriter.Write(packet, message.Question);
    }
}
