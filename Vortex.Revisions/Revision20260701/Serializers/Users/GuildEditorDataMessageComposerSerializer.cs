using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class GuildEditorDataMessageComposerSerializer(int header)
    : AbstractSerializer<GuildEditorDataMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, GuildEditorDataMessageComposer message)
    {
        GroupEditorDataSnapshot data = message.Data;

        packet.WriteInteger(data.BaseParts.Count);
        foreach (GroupBadgePartOptionSnapshot part in data.BaseParts)
        {
            packet.WriteInteger(part.Id);
            packet.WriteString(part.FileName);
            packet.WriteString(part.MaskFileName);
        }

        packet.WriteInteger(data.LayerParts.Count);
        foreach (GroupBadgePartOptionSnapshot part in data.LayerParts)
        {
            packet.WriteInteger(part.Id);
            packet.WriteString(part.FileName);
            packet.WriteString(part.MaskFileName);
        }

        packet.WriteInteger(data.BadgeColors.Count);
        foreach (GroupColorOptionSnapshot color in data.BadgeColors)
        {
            packet.WriteInteger(color.Id);
            packet.WriteString(color.ColorHex);
        }

        packet.WriteInteger(data.PrimaryColors.Count);
        foreach (GroupColorOptionSnapshot color in data.PrimaryColors)
        {
            packet.WriteInteger(color.Id);
            packet.WriteString(color.ColorHex);
        }

        packet.WriteInteger(data.SecondaryColors.Count);
        foreach (GroupColorOptionSnapshot color in data.SecondaryColors)
        {
            packet.WriteInteger(color.Id);
            packet.WriteString(color.ColorHex);
        }
    }
}
