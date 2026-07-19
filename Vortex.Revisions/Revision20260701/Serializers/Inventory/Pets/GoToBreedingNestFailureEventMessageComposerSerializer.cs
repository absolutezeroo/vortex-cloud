using Vortex.Primitives.Messages.Outgoing.Inventory.Pets;
using Vortex.Primitives.Packets;

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
