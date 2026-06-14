namespace Turbo.Primitives.Observability;

/// <summary>Top-level grouping of audit records; maps to the durable audit category column.</summary>
public enum AuditCategory
{
    Auth,
    Staff,
    Moderation,
    Economy,
    Item,
    Room,
    Security,
    Social,
    System,
}

/// <summary>Severity of an audit record, used for incident triage and retention policy.</summary>
public enum AuditSeverity
{
    Info,
    Notice,
    Warning,
    Critical,
}

/// <summary>Outcome of the audited action.</summary>
public enum AuditResult
{
    Success,
    Failed,
    Denied,
}
