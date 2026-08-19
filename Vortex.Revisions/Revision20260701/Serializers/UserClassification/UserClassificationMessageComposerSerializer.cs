using Vortex.Primitives.Messages.Outgoing.Userclassification;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.UserClassification;

internal class UserClassificationMessageComposerSerializer(int header)
    : AbstractSerializer<UserClassificationMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        UserClassificationMessageComposer message
    )
    {
        // _SafeCls_3183.parse: count, then (userId, userName, classType) per entry. The client
        // splits them into two maps keyed by id, so both strings are mandatory per row.
        packet.WriteInteger(message.Entries.Length);

        foreach (UserClassificationEntry entry in message.Entries)
        {
            packet.WriteInteger(entry.UserId).WriteString(entry.UserName).WriteString(entry.Label);
        }
    }
}
