using Vortex.Protocol.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class ChatReviewSessionResultsMessageComposerSerializer(int header)
    : AbstractSerializer<ChatReviewSessionResultsMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ChatReviewSessionResultsMessageComposer message
    )
    {
        // Winning vote, then the reader's own, then the tally -- the order the client's parser reads
        // them, and the reason the packet is composed per recipient rather than once for everyone.
        packet.WriteInteger(message.WinningVote).WriteInteger(message.OwnVote);

        packet.WriteInteger(message.FinalStatuses.Length);

        foreach (int status in message.FinalStatuses)
        {
            packet.WriteInteger(status);
        }
    }
}
