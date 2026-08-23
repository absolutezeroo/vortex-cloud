using Vortex.Protocol.Messages.Outgoing.Moderation;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

internal class IssuePickFailedMessageComposerSerializer(int header)
    : AbstractSerializer<IssuePickFailedMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, IssuePickFailedMessageComposer message)
    {
        packet.WriteInteger(message.Conflicts.Length);

        foreach (IssuePickConflict conflict in message.Conflicts)
        {
            packet
                .WriteInteger(conflict.IssueId)
                .WriteInteger(conflict.PickerUserId)
                .WriteString(conflict.PickerUserName);
        }

        packet.WriteBoolean(message.RetryEnabled).WriteInteger(message.RetryCount);
    }
}
