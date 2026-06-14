namespace Turbo.Observability.Configuration;

/// <summary>
/// Strongly-typed options for the observability brick, bound from the <c>Turbo:Observability</c>
/// configuration section following the project's per-module options convention.
/// </summary>
public sealed class ObservabilityConfig
{
    public const string SECTION_NAME = "Turbo:Observability";

    /// <summary>Master switch for emitting <c>System.Diagnostics.Metrics</c> instruments.</summary>
    public bool MetricsEnabled { get; init; } = true;

    /// <summary>
    /// When true, an <c>Activity</c> span is started per operation (OpenTelemetry-ready). Spans are
    /// near-free when no listener is registered, so this can stay on by default.
    /// </summary>
    public bool TracingEnabled { get; init; } = true;

    /// <summary>Bounded capacity of the in-process audit queue before saturation drops occur.</summary>
    public int AuditChannelCapacity { get; init; } = 10_000;

    /// <summary>Maximum number of audit events persisted per database round-trip.</summary>
    public int AuditBatchSize { get; init; } = 200;

    /// <summary>Sliding window in seconds for live `/api/overview` aggregates.</summary>
    public int LiveStatsWindowSeconds { get; init; } = 60;

    /// <summary>Maximum number of top entries returned for live rooms/abusers.</summary>
    public int LiveStatsTopK { get; init; } = 5;

    /// <summary>Retry attempts when durable audit batch persistence fails.</summary>
    public int AuditWriteRetryAttempts { get; init; } = 2;

    /// <summary>Delay between retry attempts for failed durable audit writes.</summary>
    public int AuditWriteRetryDelayMs { get; init; } = 250;

    /// <summary>Path to dead-letter file for batches that cannot be written to DB.</summary>
    public string AuditDeadLetterPath { get; init; } = "logs/audit-dead-letter.jsonl";

    /// <summary>
    /// Enables the native admin dashboard HTTP API. Off by default; it additionally refuses to start
    /// unless <see cref="DashboardToken"/> is set, so there is never anonymous admin access.
    /// </summary>
    public bool DashboardEnabled { get; init; }

    /// <summary>Host the dashboard binds to. Keep "localhost" unless a reverse proxy fronts it.</summary>
    public string DashboardHost { get; init; } = "localhost";

    /// <summary>TCP port for the dashboard HTTP API.</summary>
    public int DashboardPort { get; init; } = 9000;

    /// <summary>
    /// Shared secret required on every dashboard request (header <c>X-Admin-Token</c> or
    /// <c>?token=</c>). Empty disables the dashboard.
    /// </summary>
    public string DashboardToken { get; init; } = string.Empty;

    /// <summary>Token that grants full admin access.</summary>
    public string DashboardAdminToken { get; init; } = string.Empty;

    /// <summary>Token that grants economy-view access.</summary>
    public string DashboardEconomyToken { get; init; } = string.Empty;

    /// <summary>Token that grants moderator-level access.</summary>
    public string DashboardModeratorToken { get; init; } = string.Empty;
}
