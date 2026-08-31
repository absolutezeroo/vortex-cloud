using Vortex.Primitives.Fishing;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Fishing;

namespace Vortex.Revisions.Revision20260701.Serializers.Fishing;

/// <summary>
/// Field order here is the contract with vortex-modern-client's
/// VortexFishingRecordsMessageParser — keep the two in lockstep, and only ever append.
/// </summary>
internal class VortexFishingRecordsMessageComposerSerializer(int header)
    : AbstractSerializer<VortexFishingRecordsMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        VortexFishingRecordsMessageComposer message
    )
    {
        packet.WriteInteger(message.Records.Count);

        foreach (FishingRecordSnapshot record in message.Records)
        {
            packet
                .WriteInteger(record.SpeciesId)
                .WriteInteger(record.BestWeight)
                .WriteInteger(record.CaughtCount)
                .WriteInteger(record.BestAt);
        }
    }
}
