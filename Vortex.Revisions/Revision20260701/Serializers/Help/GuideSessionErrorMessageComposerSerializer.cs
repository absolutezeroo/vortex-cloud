using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Help;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class GuideSessionErrorMessageComposerSerializer(int header)
    : AbstractSerializer<GuideSessionErrorMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuideSessionErrorMessageComposer message
    ) => packet.WriteInteger(message.ErrorCode);
}
