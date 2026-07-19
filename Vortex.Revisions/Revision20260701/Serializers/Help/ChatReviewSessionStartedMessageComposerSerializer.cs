using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class ChatReviewSessionStartedMessageComposerSerializer(int header)
    : AbstractSerializer<ChatReviewSessionStartedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ChatReviewSessionStartedMessageComposer message
    )
    {
        //
    }
}
