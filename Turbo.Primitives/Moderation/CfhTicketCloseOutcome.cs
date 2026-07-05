namespace Turbo.Primitives.Moderation;

public readonly record struct CfhTicketCloseOutcome(
    int IssueId,
    int ReporterPlayerId,
    int ReportedPlayerId,
    bool Sanctioned
);
