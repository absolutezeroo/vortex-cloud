using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Fishing;

namespace Vortex.Revisions.Revision20260701.Serializers.Fishing;

/// <summary>
/// Field order here is the contract with vortex-modern-client's
/// VortexFishingPlayerStateMessageParser — keep the two in lockstep, and only ever append.
/// </summary>
internal class VortexFishingPlayerStateMessageComposerSerializer(int header)
    : AbstractSerializer<VortexFishingPlayerStateMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        VortexFishingPlayerStateMessageComposer message
    )
    {
        packet
            .WriteInteger(message.FishingLevel)
            .WriteInteger(message.FishingXp)
            .WriteInteger(message.RodQuality)
            .WriteInteger(message.RodXp)
            .WriteInteger(message.Currency)
            .WriteInteger(message.CurrencyEarnedToday)
            .WriteInteger(message.DailyCap)
            .WriteInteger(message.SessionCatchCount)
            .WriteInteger(message.CollectibleIds.Count);

        foreach (int collectibleId in message.CollectibleIds)
        {
            packet.WriteInteger(collectibleId);
        }
    }
}
