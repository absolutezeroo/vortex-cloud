using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents;

internal class WiredEnvironmentMessageComposerSerializer(int header)
    : AbstractSerializer<WiredEnvironmentMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, WiredEnvironmentMessageComposer message)
    {
        // The client's parser reads the boolean, then reads the count only if any bytes are left.
        // The count is written unconditionally anyway: "no achievements" and "the sender stopped
        // early" are the same bytes otherwise, and a reader that has to guess which one it got is
        // exactly the ambiguity a length prefix exists to remove.
        packet
            .WriteBoolean(message.HasClickUserWired)
            .WriteInteger(message.EnabledAchievements.Count);

        foreach (string achievement in message.EnabledAchievements)
        {
            packet.WriteString(achievement);
        }
    }
}
