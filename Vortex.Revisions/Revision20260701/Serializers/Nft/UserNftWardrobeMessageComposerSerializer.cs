using Vortex.Primitives.Messages.Outgoing.Nft;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Nft;

internal class UserNftWardrobeMessageComposerSerializer(int header)
    : AbstractSerializer<UserNftWardrobeMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, UserNftWardrobeMessageComposer message)
    {
        //
    }
}
