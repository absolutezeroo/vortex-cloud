using Vortex.Protocol.Messages.Outgoing.Room.Bots;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Bots;

internal class BotCommandConfigurationMessageComposerSerializer(int header)
    : AbstractSerializer<BotCommandConfigurationMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        BotCommandConfigurationMessageComposer message
    ) =>
        packet
            .WriteInteger(message.BotId)
            .WriteInteger(message.CommandId)
            .WriteString(message.Data);
}
