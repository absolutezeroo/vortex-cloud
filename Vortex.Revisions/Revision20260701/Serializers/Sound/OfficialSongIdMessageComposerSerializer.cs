using Vortex.Primitives.Messages.Outgoing.Sound;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Sound;

internal class OfficialSongIdMessageComposerSerializer(int header)
    : AbstractSerializer<OfficialSongIdMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, OfficialSongIdMessageComposer message)
    {
        //
    }
}
