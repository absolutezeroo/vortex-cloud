using Vortex.Primitives.Messages.Outgoing.Moderation;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

internal class CfhChatlogEventMessageComposerSerializer(int header)
    : AbstractSerializer<CfhChatlogEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, CfhChatlogEventMessageComposer message)
    {
        packet
            .WriteInteger(message.CallId)
            .WriteInteger(message.CallerUserId)
            .WriteInteger(message.ReportedUserId)
            .WriteInteger(message.ChatRecordId);

        ChatlogSerialization.WriteBlock(packet, message.ChatRecord);
    }
}
