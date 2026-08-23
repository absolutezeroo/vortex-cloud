using Vortex.Protocol.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class ChatReviewSessionVotingStatusMessageComposerSerializer(int header)
    : AbstractSerializer<ChatReviewSessionVotingStatusMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ChatReviewSessionVotingStatusMessageComposer message
    )
    {
        packet.WriteInteger(message.Statuses.Length);

        foreach (int status in message.Statuses)
        {
            packet.WriteInteger(status);
        }
    }
}
