using System.Linq;
using Vortex.Primitives.Messages.Outgoing.Perk;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Perk;

internal class PerkAllowancesMessageComposerSerializer(int header)
    : AbstractSerializer<PerkAllowancesMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, PerkAllowancesMessageComposer message)
    {
        packet.WriteInteger(message.Perks.Count());

        foreach (PerkAllowanceItem perk in message.Perks)
        {
            packet
                .WriteString(perk.Code)
                .WriteString(perk.ErrorMessage)
                .WriteBoolean(perk.IsAllowed);
        }
    }
}
