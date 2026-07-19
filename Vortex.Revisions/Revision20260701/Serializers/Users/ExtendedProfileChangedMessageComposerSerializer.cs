using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class ExtendedProfileChangedMessageComposerSerializer(int header)
    : AbstractSerializer<ExtendedProfileChangedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ExtendedProfileChangedMessageComposer message
    )
    {
        packet.WriteInteger(message.UserId);
    }
}
