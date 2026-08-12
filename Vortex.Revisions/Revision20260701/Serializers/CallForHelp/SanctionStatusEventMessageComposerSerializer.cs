using Vortex.Primitives.Messages.Outgoing.Callforhelp;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.CallForHelp;

internal class SanctionStatusEventMessageComposerSerializer(int header)
    : AbstractSerializer<SanctionStatusEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        SanctionStatusEventMessageComposer message
    )
    {
        packet.WriteInteger(message.Sanctions.Length);

        foreach (SanctionRecord sanction in message.Sanctions)
        {
            // Not symmetrical: the sanction being served comes first, the one that would come next
            // comes last, with the three scalars between them.
            SerializeType(packet, sanction.SanctionType);

            packet
                .WriteString(sanction.Description)
                .WriteBoolean(sanction.ShowsProbationDetails)
                .WriteInteger(sanction.ProbationHoursLeft);

            SerializeType(packet, sanction.NextSanctionType);
        }
    }

    private static void SerializeType(IServerPacket packet, SanctionType type)
    {
        packet
            .WriteString(type.Name)
            .WriteInteger(type.DurationHours)
            .WriteInteger(type.ProbationHours);
    }
}
