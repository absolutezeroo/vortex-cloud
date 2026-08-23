using Vortex.Protocol.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class GuideSessionStartedMessageComposerSerializer(int header)
    : AbstractSerializer<GuideSessionStartedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuideSessionStartedMessageComposer message
    )
    {
        // Requester first, then guide -- both as id, name, figure.
        packet
            .WriteInteger(message.RequesterId)
            .WriteString(message.RequesterName)
            .WriteString(message.RequesterFigure)
            .WriteInteger(message.GuideId)
            .WriteString(message.GuideName)
            .WriteString(message.GuideFigure);
    }
}
