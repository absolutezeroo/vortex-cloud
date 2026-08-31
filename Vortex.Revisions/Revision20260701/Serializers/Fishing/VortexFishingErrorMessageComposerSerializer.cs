using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Fishing;

namespace Vortex.Revisions.Revision20260701.Serializers.Fishing;

/// <summary>
/// Field order here is the contract with vortex-modern-client's VortexFishingErrorMessageParser —
/// keep the two in lockstep, and only ever append.
/// </summary>
internal class VortexFishingErrorMessageComposerSerializer(int header)
    : AbstractSerializer<VortexFishingErrorMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        VortexFishingErrorMessageComposer message
    )
    {
        packet.WriteInteger(message.Code);
        packet.WriteInteger(message.Detail);
    }
}
