using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Bots;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Bots;

internal class BotForceOpenContextMenuMessageComposerSerializer(int header)
    : AbstractSerializer<BotForceOpenContextMenuMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        BotForceOpenContextMenuMessageComposer message
    ) => packet.WriteInteger(message.BotId);
}
