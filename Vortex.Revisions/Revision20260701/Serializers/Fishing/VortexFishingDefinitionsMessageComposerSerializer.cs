using Vortex.Primitives.Fishing;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Fishing;

namespace Vortex.Revisions.Revision20260701.Serializers.Fishing;

/// <summary>
/// Field order here is the contract with vortex-modern-client's
/// VortexFishingDefinitionsMessageParser — keep the two in lockstep.
/// </summary>
/// <remarks>
/// Three nested, count-prefixed tables with no framing inside them. A field written in the wrong
/// order does not fail on either side: it shifts everything after it, and the client ends up holding
/// plausible nonsense. Fields are append-only for the same reason — a new one goes at the end of its
/// record, never in the middle.
/// </remarks>
internal class VortexFishingDefinitionsMessageComposerSerializer(int header)
    : AbstractSerializer<VortexFishingDefinitionsMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        VortexFishingDefinitionsMessageComposer message
    )
    {
        packet.WriteInteger(message.Version);

        packet.WriteInteger(message.Species.Count);

        foreach (FishSpeciesSnapshot species in message.Species)
        {
            packet
                .WriteInteger(species.Id)
                .WriteString(species.NameKey)
                .WriteInteger(species.ZoneId)
                .WriteInteger(species.RequiredLevel)
                .WriteInteger(species.RarityStars)
                .WriteInteger(species.CatchRate)
                .WriteInteger(species.RarityWeight)
                .WriteInteger(species.MinWeight)
                .WriteInteger(species.MaxWeight)
                .WriteInteger(species.XpReward)
                .WriteInteger(species.GoldenXpBonus)
                .WriteInteger(species.CurrencyReward)
                .WriteInteger(species.ActiveHours)
                .WriteInteger(species.ActiveWeekdays)
                .WriteInteger(species.ActiveSeasons);
        }

        packet.WriteInteger(message.RodLevels.Count);

        foreach (FishingRodLevelSnapshot level in message.RodLevels)
        {
            packet
                .WriteInteger(level.Quality)
                .WriteInteger(level.XpThreshold)
                .WriteString(level.NameKey)
                .WriteInteger(level.HandItemId)
                .WriteInteger(level.CatchMultiplier)
                .WriteInteger(level.GoldenMultiplier)
                .WriteInteger(level.HookHavocChance);
        }

        packet.WriteInteger(message.FishingLevels.Count);

        foreach (FishingLevelSnapshot level in message.FishingLevels)
        {
            packet.WriteInteger(level.Level).WriteInteger(level.XpThreshold);
        }

        packet.WriteInteger(message.Zones.Count);

        foreach (FishingZoneSnapshot zone in message.Zones)
        {
            packet
                .WriteInteger(zone.Id)
                .WriteString(zone.NameKey)
                .WriteString(zone.FurniClass)
                .WriteInteger(zone.RequiredLevel)
                .WriteInteger(zone.MinCatches)
                .WriteInteger(zone.MaxCatches);
        }
    }
}
