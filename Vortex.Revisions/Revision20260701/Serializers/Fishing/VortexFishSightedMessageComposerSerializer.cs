using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Fishing;

namespace Vortex.Revisions.Revision20260701.Serializers.Fishing;

/// <summary>
/// Field order here is the contract with vortex-modern-client's VortexFishSightedMessageParser —
/// keep the two in lockstep, and only ever append.
/// </summary>
internal class VortexFishSightedMessageComposerSerializer(int header)
    : AbstractSerializer<VortexFishSightedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        VortexFishSightedMessageComposer message
    ) =>
        packet
            .WriteInteger(message.SightingId)
            .WriteInteger(message.SpotItemId)
            .WriteBoolean(message.Golden)
            .WriteInteger(message.DurationMs);
}
