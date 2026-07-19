using Vortex.Primitives.Messages.Outgoing.Moderation;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

internal class ModeratorMessageComposerSerializer(int header)
    : AbstractSerializer<ModeratorMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ModeratorMessageComposer message)
    {
        //
    }
}
