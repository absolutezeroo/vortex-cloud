using Vortex.Primitives.Messages.Outgoing.Avatar;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums;

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
