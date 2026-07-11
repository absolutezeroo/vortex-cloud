using Turbo.Primitives.Messages.Outgoing.Moderation;
using Turbo.Primitives.Moderation;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Serializers.Moderation;

internal class ModeratorInitMessageComposerSerializer(int header)
    : AbstractSerializer<ModeratorInitMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ModeratorInitMessageComposer message)
    {
        packet.WriteInteger(message.Issues.Length);

        foreach (CfhIssueQueueEntrySnapshot issue in message.Issues)
        {
            packet
                .WriteInteger(issue.IssueId)
                .WriteInteger((int)issue.State)
                .WriteInteger(issue.CategoryId)
                .WriteInteger(issue.CategoryId) // reportedCategoryId: not tracked separately
                .WriteInteger(issue.IssueAgeMs)
                .WriteInteger(issue.Priority)
                .WriteInteger(issue.IssueId) // groupingId: no server-side ticket bundling
                .WriteInteger(issue.ReporterUserId)
                .WriteString(issue.ReporterUserName)
                .WriteInteger(issue.ReportedUserId)
                .WriteString(issue.ReportedUserName)
                .WriteInteger(issue.PickerUserId)
                .WriteString(issue.PickerUserName)
                .WriteString(issue.Message)
                .WriteInteger(issue.IssueId) // chatRecordId: GetCfhChatlogMessage resolves by issueId
                .WriteInteger(0); // patternCount: keyword-highlight evidence, out of scope
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
