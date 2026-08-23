using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Protocol.Messages.Outgoing.Avatar;

namespace Vortex.Revisions.Revision20260701.Serializers.Avatar;

internal class FigureUpdateEventMessageComposerSerializer(int header)
    : AbstractSerializer<FigureUpdateEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        FigureUpdateEventMessageComposer message
    )
    {
        packet.WriteString(message.Figure).WriteString(message.Gender.ToLegacyString());
    }
}
