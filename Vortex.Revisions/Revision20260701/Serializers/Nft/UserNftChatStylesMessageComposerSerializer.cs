using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Nft;

namespace Vortex.Revisions.Revision20260701.Serializers.Nft;

internal class UserNftChatStylesMessageComposerSerializer(int header)
    : AbstractSerializer<UserNftChatStylesMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        UserNftChatStylesMessageComposer message
    )
    {
        packet.WriteInteger(message.ChatStyleIds.Count);

        foreach (int chatStyleId in message.ChatStyleIds)
        {
            packet.WriteInteger(chatStyleId);
        }
    }
}
