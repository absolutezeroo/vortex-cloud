using Vortex.Primitives.Fishing;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Fishing;

namespace Vortex.Revisions.Revision20260701.Serializers.Fishing;

/// <summary>
/// Field order here is the contract with vortex-modern-client's
/// VortexFishingDerbyStandingMessageParser — keep the two in lockstep, and only ever append.
/// </summary>
/// <remarks>
/// The own-rank field comes <em>after</em> the entry list, not before it. The client reads it that
/// way, and a count-prefixed list with a field on the far side of it is the one shape where getting
/// the order wrong still parses — into plausible nonsense.
/// </remarks>
internal class VortexFishingDerbyStandingMessageComposerSerializer(int header)
    : AbstractSerializer<VortexFishingDerbyStandingMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        VortexFishingDerbyStandingMessageComposer message
    )
    {
        packet
            .WriteInteger(message.DerbyId)
            .WriteInteger(message.EndsAt)
            .WriteInteger(message.Entries.Count);

        foreach (FishingDerbyEntrySnapshot entry in message.Entries)
        {
            packet
                .WriteInteger(entry.PlayerId)
                .WriteString(entry.PlayerName)
                .WriteInteger(entry.Score);
        }

        packet.WriteInteger(message.OwnRank);
    }
}
