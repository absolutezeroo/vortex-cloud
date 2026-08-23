using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Notifications;

namespace Vortex.Revisions.Revision20260701.Serializers.Notifications;

internal class RestoreClientMessageComposerSerializer(int header)
    : AbstractSerializer<RestoreClientMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, RestoreClientMessageComposer message)
    {
        //
    }
}
