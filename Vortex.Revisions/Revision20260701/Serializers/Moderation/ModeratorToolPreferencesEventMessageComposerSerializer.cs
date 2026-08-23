using Vortex.Protocol.Messages.Outgoing.Moderation;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

internal class ModeratorToolPreferencesEventMessageComposerSerializer(int header)
    : AbstractSerializer<ModeratorToolPreferencesEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ModeratorToolPreferencesEventMessageComposer message
    )
    {
        packet
            .WriteInteger(message.WindowX)
            .WriteInteger(message.WindowY)
            .WriteInteger(message.WindowWidth)
            .WriteInteger(message.WindowHeight);
    }
}
