using Vortex.Primitives.Messages.Outgoing.Sound;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Sound;

internal class TraxSongInfoMessageComposerSerializer(int header)
    : AbstractSerializer<TraxSongInfoMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, TraxSongInfoMessageComposer message)
    {
        //
    }
}
