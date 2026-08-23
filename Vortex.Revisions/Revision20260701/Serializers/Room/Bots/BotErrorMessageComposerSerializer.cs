using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Bots;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Bots;

internal class BotErrorMessageComposerSerializer(int header)
    : AbstractSerializer<BotErrorMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, BotErrorMessageComposer message) =>
        packet.WriteInteger(message.ErrorCode);
}
