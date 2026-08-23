using Vortex.Protocol.Messages.Outgoing.Poll;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Polls.Snapshots;

namespace Vortex.Revisions.Revision20260701.Serializers.Poll;

/// <summary>Question id, then the closing answer→count tally.</summary>
internal class QuestionFinishedEventMessageComposerSerializer(int header)
    : AbstractSerializer<QuestionFinishedEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        QuestionFinishedEventMessageComposer message
    )
    {
        packet.WriteInteger(message.QuestionId);
        packet.WriteInteger(message.AnswerCounts.Length);

        foreach (PollAnswerCountSnapshot count in message.AnswerCounts)
        {
            packet.WriteString(count.Answer);
            packet.WriteInteger(count.Count);
        }
    }
}
