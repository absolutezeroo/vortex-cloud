using Vortex.Primitives.Moderation;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Moderation;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

internal class ModeratorInitMessageComposerSerializer(int header)
    : AbstractSerializer<ModeratorInitMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ModeratorInitMessageComposer message)
    {
        packet.WriteInteger(message.Issues.Length);

        foreach (CfhIssueQueueEntrySnapshot issue in message.Issues)
        {
            IssueSerialization.WriteIssue(packet, issue);
        }

        packet.WriteInteger(message.MessageTemplates.Length);

        foreach (string template in message.MessageTemplates)
        {
            packet.WriteString(template);
        }

        packet.WriteInteger(0); // unused string list per the client parser

        packet
            .WriteBoolean(message.CfhPermission)
            .WriteBoolean(message.ChatlogsPermission)
            .WriteBoolean(message.AlertPermission)
            .WriteBoolean(message.KickPermission)
            .WriteBoolean(message.BanPermission)
            .WriteBoolean(message.RoomAlertPermission)
            .WriteBoolean(message.RoomKickPermission);

        packet.WriteInteger(message.RoomMessageTemplates.Length);

        foreach (string template in message.RoomMessageTemplates)
        {
            packet.WriteString(template);
        }
    }
}
