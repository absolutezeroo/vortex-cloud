using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Habbicons;

namespace Vortex.Revisions.Revision20260701.Serializers.Habbicons;

/// <summary>The client's <c>_SafeCls_4372.parse</c>: habbiconId then state, both integers.</summary>
internal class UserHabbiconStatusChangedMessageComposerSerializer(int header)
    : AbstractSerializer<UserHabbiconStatusChangedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        UserHabbiconStatusChangedMessageComposer message
    ) => packet.WriteInteger(message.HabbiconId).WriteInteger((int)message.State);
}
