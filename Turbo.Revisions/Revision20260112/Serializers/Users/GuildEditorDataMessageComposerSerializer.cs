using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Serializers.Users;

internal class GuildEditorDataMessageComposerSerializer(int header)
    : AbstractSerializer<GuildEditorDataMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, GuildEditorDataMessageComposer message)
    {
        var data = message.Data;

        packet.WriteInteger(data.BaseParts.Count);
        foreach (var part in data.BaseParts)
        {
            packet.WriteInteger(part.Id);
            packet.WriteString(part.FileName);
            packet.WriteString(part.MaskFileName);
        }

        packet.WriteInteger(data.LayerParts.Count);
        foreach (var part in data.LayerParts)
        {
            packet.WriteInteger(part.Id);
            packet.WriteString(part.FileName);
            packet.WriteString(part.MaskFileName);
        }

        packet.WriteInteger(data.BadgeColors.Count);
        foreach (var color in data.BadgeColors)
        {
            packet.WriteInteger(color.Id);
            packet.WriteString(color.ColorHex);
        }

        packet.WriteInteger(data.PrimaryColors.Count);
        foreach (var color in data.PrimaryColors)
        {
            packet.WriteInteger(color.Id);
            packet.WriteString(color.ColorHex);
        }

        packet.WriteInteger(data.SecondaryColors.Count);
        foreach (var color in data.SecondaryColors)
        {
            packet.WriteInteger(color.Id);
            packet.WriteString(color.ColorHex);
        }
    }
}
