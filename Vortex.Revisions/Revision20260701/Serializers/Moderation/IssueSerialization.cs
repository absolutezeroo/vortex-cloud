using Vortex.Primitives.Moderation;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

/// <summary>
/// Shared wire writer for the WIN63 "issue block" (decoded from the client as class_3400's 16-field
/// constructor, filled in order by _SafeCls_3702::parse). ModeratorInitMessageComposer repeats it
/// per queue entry and IssueInfoMessageComposer sends exactly one, so the two must not drift.
/// </summary>
internal static class IssueSerialization
{
    public static void WriteIssue(IServerPacket packet, CfhIssueQueueEntrySnapshot issue)
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
}
