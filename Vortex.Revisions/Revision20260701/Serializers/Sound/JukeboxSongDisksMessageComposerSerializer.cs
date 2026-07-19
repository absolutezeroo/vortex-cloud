using Vortex.Primitives.Messages.Outgoing.Sound;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Sound;

internal class JukeboxSongDisksMessageComposerSerializer(int header)
    : AbstractSerializer<JukeboxSongDisksMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, JukeboxSongDisksMessageComposer message)
    {
        //
    }
}
