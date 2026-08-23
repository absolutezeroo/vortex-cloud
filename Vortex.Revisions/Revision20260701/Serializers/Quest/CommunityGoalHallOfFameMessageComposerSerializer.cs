using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Quest;

namespace Vortex.Revisions.Revision20260701.Serializers.Quest;

internal class CommunityGoalHallOfFameMessageComposerSerializer(int header)
    : AbstractSerializer<CommunityGoalHallOfFameMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CommunityGoalHallOfFameMessageComposer message
    )
    {
        packet.WriteString(message.GoalCode).WriteInteger(message.Entries.Length);

        foreach (CommunityGoalHallOfFameEntry entry in message.Entries)
        {
            packet
                .WriteInteger(entry.UserId)
                .WriteString(entry.UserName)
                .WriteString(entry.Figure)
                .WriteInteger(entry.Rank)
                .WriteInteger(entry.CurrentScore);
        }
    }
}
