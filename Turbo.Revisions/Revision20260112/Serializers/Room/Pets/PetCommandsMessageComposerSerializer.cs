using Turbo.Primitives.Messages.Outgoing.Room.Pets;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Serializers.Room.Pets;

internal class PetCommandsMessageComposerSerializer(int header)
    : AbstractSerializer<PetCommandsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, PetCommandsMessageComposer message)
    {
        packet.WriteInteger(message.PetId).WriteInteger(message.AllCommandIds.Length);

        foreach (int id in message.AllCommandIds)
        {
            packet.WriteInteger(id);
        }

        packet.WriteInteger(message.EnabledCommandIds.Length);

        foreach (int id in message.EnabledCommandIds)
        {
            packet.WriteInteger(id);
        }
    }
}
