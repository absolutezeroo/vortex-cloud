using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Fishing;

namespace Vortex.Revisions.Revision20260701.Serializers.Fishing;

/// <summary>
/// Field order here is the contract with vortex-modern-client's
/// VortexFishingCatchResultMessageParser — keep the two in lockstep, and only ever append.
/// </summary>
internal class VortexFishingCatchResultMessageComposerSerializer(int header)
    : AbstractSerializer<VortexFishingCatchResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        VortexFishingCatchResultMessageComposer message
    ) =>
        packet
            .WriteInteger(message.RecordId)
            .WriteInteger(message.SpeciesId)
            .WriteInteger(message.Weight)
            .WriteInteger(message.XpGained)
            .WriteInteger(message.CurrencyGained)
            .WriteBoolean(message.Golden)
            .WriteInteger(message.NewLevel);
}
