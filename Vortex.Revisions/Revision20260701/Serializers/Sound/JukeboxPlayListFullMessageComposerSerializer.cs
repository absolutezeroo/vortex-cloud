using Vortex.Primitives.Messages.Outgoing.Sound;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Sound;

internal class JukeboxPlayListFullMessageComposerSerializer(int header)
    : AbstractSerializer<JukeboxPlayListFullMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        JukeboxPlayListFullMessageComposer message
    )
    {
        //
    }
}
