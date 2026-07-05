namespace Turbo.Primitives.Moderation;

/// <summary>Matches the WIN63 client's class_3291 field order exactly — this is the per-ticket shape
/// sent in the staff mod tool's issue queue (ModeratorInitMessageEvent). Keyword-highlight "patterns"
/// are always sent empty (cosmetic evidence highlighting, out of scope). GroupingId is the ticket's
/// own id — the server does no ticket bundling, so nothing ever shares a group.</summary>
public readonly record struct CfhIssueQueueEntrySnapshot(
    int IssueId,
    CfhTicketState State,
    int CategoryId,
    int IssueAgeMs,
    int Priority,
    int ReporterUserId,
    string ReporterUserName,
    int ReportedUserId,
    string ReportedUserName,
    int PickerUserId,
    string PickerUserName,
    string Message
);
