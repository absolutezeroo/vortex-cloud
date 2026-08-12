using Vortex.Primitives.Messages.Outgoing.Poll;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Polls.Snapshots;

namespace Vortex.Revisions.Revision20260701.Serializers.Poll;

/// <summary>
/// Writes the survey: id, the two framing messages, then the root questions — each immediately
/// followed by its own follow-up count and blocks — and finally the NPS flag.
/// </summary>
internal class PollContentsEventMessageComposerSerializer(int header)
    : AbstractSerializer<PollContentsEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        PollContentsEventMessageComposer message
    )
    {
        packet.WriteInteger(message.PollId);
        packet.WriteString(message.StartMessage);
        packet.WriteString(message.EndMessage);
        packet.WriteInteger(message.Questions.Length);

        foreach (PollQuestionSnapshot question in message.Questions)
        {
            PollQuestionWriter.Write(packet, question);

            packet.WriteInteger(question.Children.Length);

            foreach (PollQuestionSnapshot child in question.Children)
            {
                PollQuestionWriter.Write(packet, child);
            }
        }

        packet.WriteBoolean(message.NpsPoll);
    }
}
