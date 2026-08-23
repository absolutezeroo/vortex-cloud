using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Competition;

namespace Vortex.Revisions.Revision20260701.Serializers.Competition;

internal class NoOwnedRoomsAlertMessageComposerSerializer(int header)
    : AbstractSerializer<NoOwnedRoomsAlertMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        NoOwnedRoomsAlertMessageComposer message
    )
    {
        //
    }
}
