using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator;

internal class OfficialRoomsMessageComposerSerializer(int header)
    : AbstractSerializer<OfficialRoomsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, OfficialRoomsMessageComposer message)
    {
        //
    }
}
