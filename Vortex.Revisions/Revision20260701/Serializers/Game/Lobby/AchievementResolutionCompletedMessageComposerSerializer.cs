using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Game.Lobby;

namespace Vortex.Revisions.Revision20260701.Serializers.Game.Lobby;

internal class AchievementResolutionCompletedMessageComposerSerializer(int header)
    : AbstractSerializer<AchievementResolutionCompletedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        AchievementResolutionCompletedMessageComposer message
    )
    {
        // Stuff first, badge second. The client's own handler reads them in the other order when it
        // forwards them to the view, which is a good way to get this backwards.
        packet.WriteString(message.StuffCode).WriteString(message.BadgeCode);
    }
}
