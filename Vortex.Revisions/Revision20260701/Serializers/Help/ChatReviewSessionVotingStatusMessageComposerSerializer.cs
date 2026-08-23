using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Help;

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
