using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Preferences;

namespace Vortex.Revisions.Revision20260701.Serializers.Preferences;

internal class DiscordPreferencesEventMessageComposerSerializer(int header)
    : AbstractSerializer<DiscordPreferencesEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        DiscordPreferencesEventMessageComposer message
    ) =>
        packet
            .WriteInteger(message.Version)
            .WriteBoolean(message.ShowHabbo)
            .WriteBoolean(message.ShareActivity)
            .WriteBoolean(message.HideInHiddenRooms)
            .WriteBoolean(message.AllowJoining);
}
