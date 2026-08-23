using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Sound;

namespace Vortex.Revisions.Revision20260701.Serializers.Sound;

internal class JukeboxSongDisksMessageComposerSerializer(int header)
    : AbstractSerializer<JukeboxSongDisksMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, JukeboxSongDisksMessageComposer message)
    {
        //
    }
}
