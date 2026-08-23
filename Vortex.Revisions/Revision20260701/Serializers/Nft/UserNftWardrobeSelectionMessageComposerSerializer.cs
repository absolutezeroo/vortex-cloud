using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Nft;

namespace Vortex.Revisions.Revision20260701.Serializers.Nft;

internal class UserNftWardrobeSelectionMessageComposerSerializer(int header)
    : AbstractSerializer<UserNftWardrobeSelectionMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        UserNftWardrobeSelectionMessageComposer message
    ) =>
        packet
            .WriteString(message.TokenId)
            .WriteString(message.FallbackFigure)
            .WriteString(message.FallbackGender);
}
