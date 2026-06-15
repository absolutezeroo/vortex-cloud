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

    /// <summary>
    /// Optional URL template for furniture icons shown in the operations picker. Use <c>{name}</c>
    /// as the definition-name placeholder, for example
    /// <c>http://your-host/dcr/hof_furni/icons/{name}_icon.png</c>. Empty hides icons (a sprite-id
    /// tile is shown instead). When set, the icon host origin is added to the dashboard CSP
    /// <c>img-src</c> so the images are allowed to load.
    /// </summary>
    public string FurniIconUrlTemplate { get; init; } = string.Empty;

    /// <summary>Bounded capacity of the in-memory error-grouping queue.</summary>
    public int ErrorGroupingChannelCapacity { get; init; } = 10_000;

    /// <summary>Maximum number of grouped error rows persisted per writer batch.</summary>
    public int ErrorTrackingBatchSize { get; init; } = 150;

    /// <summary>Retry attempts when error-grouping writes fail.</summary>
    public int ErrorTrackingWriteRetryAttempts { get; init; } = 2;

    /// <summary>Delay between error-grouping write retries (milliseconds).</summary>
    public int ErrorTrackingWriteRetryDelayMs { get; init; } = 250;

    /// <summary>Latency threshold (ms) above which DB health is marked degraded.</summary>
    public int DatabaseProbeDegradedMs { get; init; } = 250;

    /// <summary>Latency threshold (ms) above which DB health is marked critical.</summary>
    public int DatabaseProbeCriticalMs { get; init; } = 1200;

    /// <summary>Latency threshold (ms) above which Orleans health is marked degraded.</summary>
    public int OrleansProbeDegradedMs { get; init; } = 350;

    /// <summary>Latency threshold (ms) above which Orleans health is marked critical.</summary>
    public int OrleansProbeCriticalMs { get; init; } = 1200;

    /// <summary>Incident detection rolling window in minutes.</summary>
    public int IncidentLookbackMinutes { get; init; } = 5;

    /// <summary>Degraded threshold for runtime error rate (errors/minute).</summary>
    public int ErrorSpikesDegradedPerMinute { get; init; } = 20;

    /// <summary>Critical threshold for runtime error rate (errors/minute).</summary>
    public int ErrorSpikesCriticalPerMinute { get; init; } = 80;

    /// <summary>Degraded threshold for login-failed rate (fails/minute).</summary>
    public int LoginFailedSpikesDegradedPerMinute { get; init; } = 8;

    /// <summary>Critical threshold for login-failed rate (fails/minute).</summary>
    public int LoginFailedSpikesCriticalPerMinute { get; init; } = 20;

    /// <summary>Number of top error groups returned in an incident snapshot.</summary>
    public int IncidentTopErrorGroups { get; init; } = 5;
}
