using Vortex.Primitives.Messages.Outgoing.Notifications;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Notifications;

internal class HabboBroadcastMessageComposerSerializer(int header)
    : AbstractSerializer<HabboBroadcastMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, HabboBroadcastMessageComposer message)
    {
        //
    }
}
