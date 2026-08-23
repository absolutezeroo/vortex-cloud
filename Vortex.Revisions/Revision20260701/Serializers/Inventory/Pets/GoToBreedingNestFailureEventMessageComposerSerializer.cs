using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Inventory.Pets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Pets;

internal class GoToBreedingNestFailureEventMessageComposerSerializer(int header)
    : AbstractSerializer<GoToBreedingNestFailureEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GoToBreedingNestFailureEventMessageComposer message
    )
    {
        packet.WriteInteger(message.Reason);
    }
}
