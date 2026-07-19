using Vortex.Primitives.Messages.Outgoing.Room.Session;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Session;

internal class HanditemConfigurationMessageComposerSerializer(int header)
    : AbstractSerializer<HanditemConfigurationMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        HanditemConfigurationMessageComposer message
    )
    {
        packet.WriteBoolean(message.IsHanditemControlBlocked);
    }
}
