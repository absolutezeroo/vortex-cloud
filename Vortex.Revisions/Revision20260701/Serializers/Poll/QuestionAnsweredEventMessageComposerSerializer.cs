using Vortex.Primitives.Packets;
using Vortex.Primitives.Polls.Snapshots;
using Vortex.Protocol.Messages.Outgoing.Poll;

namespace Vortex.Revisions.Revision20260701.Serializers.Poll;

/// <summary>Who answered, what they picked, then the refreshed answer→count tally.</summary>
internal class QuestionAnsweredEventMessageComposerSerializer(int header)
    : AbstractSerializer<QuestionAnsweredEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        QuestionAnsweredEventMessageComposer message
    )
    {
        packet.WriteInteger(message.UserId);
        packet.WriteString(message.Value);
        packet.WriteInteger(message.AnswerCounts.Length);

        foreach (PollAnswerCountSnapshot count in message.AnswerCounts)
        {
            packet.WriteString(count.Answer);
            packet.WriteInteger(count.Count);
        }
    }
}
