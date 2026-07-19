using Vortex.Primitives.Messages.Outgoing.Nft;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Nft;

internal class UserNftWardrobeSelectionMessageComposerSerializer(int header)
    : AbstractSerializer<UserNftWardrobeSelectionMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        UserNftWardrobeSelectionMessageComposer message
    )
    {
        //
    }
}
