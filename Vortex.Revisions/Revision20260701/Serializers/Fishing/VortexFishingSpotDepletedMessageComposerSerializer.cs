using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Fishing;

namespace Vortex.Revisions.Revision20260701.Serializers.Fishing;

/// <summary>
/// Field order here is the contract with vortex-modern-client's
/// VortexFishingSpotDepletedMessageParser — keep the two in lockstep, and only ever append.
/// </summary>
internal class VortexFishingSpotDepletedMessageComposerSerializer(int header)
    : AbstractSerializer<VortexFishingSpotDepletedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        VortexFishingSpotDepletedMessageComposer message
    ) => packet.WriteInteger(message.SpotItemId).WriteInteger(message.Catches);
}
