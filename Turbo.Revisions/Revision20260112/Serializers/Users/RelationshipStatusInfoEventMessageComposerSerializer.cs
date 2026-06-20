using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Packets;
using Turbo.Primitives.Snapshots.FriendList;

namespace Turbo.Revisions.Revision20260112.Serializers.Users;

internal class RelationshipStatusInfoEventMessageComposerSerializer(int header)
    : AbstractSerializer<RelationshipStatusInfoEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RelationshipStatusInfoEventMessageComposer message
    )
    {
        packet.WriteInteger(message.UserId);
        packet.WriteInteger(message.Relations.Count);

        foreach (RelationshipStatusEntrySnapshot rel in message.Relations)
        {
            packet.WriteShort(rel.RelationType);
            packet.WriteInteger(rel.Count);
            packet.WriteString(rel.Name);
            packet.WriteString(rel.Figure);
        }
    }
}
