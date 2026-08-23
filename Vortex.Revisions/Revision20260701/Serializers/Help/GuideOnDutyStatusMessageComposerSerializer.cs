using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Help;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class GuideOnDutyStatusMessageComposerSerializer(int header)
    : AbstractSerializer<GuideOnDutyStatusMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuideOnDutyStatusMessageComposer message
    )
    {
        // Guides, then helpers, then guardians -- the order the client's parser reads them, which is
        // not the order its own checkboxes are drawn in.
        packet
            .WriteBoolean(message.OnDuty)
            .WriteInteger(message.GuidesOnDuty)
            .WriteInteger(message.HelpersOnDuty)
            .WriteInteger(message.GuardiansOnDuty);
    }
}
