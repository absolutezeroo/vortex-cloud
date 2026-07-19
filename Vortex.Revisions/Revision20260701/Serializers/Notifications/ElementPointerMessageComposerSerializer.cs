using Vortex.Primitives.Messages.Outgoing.Notifications;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Notifications;

internal class ElementPointerMessageComposerSerializer(int header)
    : AbstractSerializer<ElementPointerMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ElementPointerMessageComposer message)
    {
        //
    }
}
