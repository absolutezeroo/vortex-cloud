using Vortex.Primitives.Messages.Outgoing.Sound;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Sound;

internal class PlayListMessageComposerSerializer(int header)
    : AbstractSerializer<PlayListMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, PlayListMessageComposer message)
    {
        //
    }
}
