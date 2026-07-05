namespace Turbo.Primitives.Moderation;

public readonly record struct CfhTicketSummary(
    int Id,
    CfhTicketState State,
    int TopicId,
    int ReporterPlayerId,
    int ReportedPlayerId
);
