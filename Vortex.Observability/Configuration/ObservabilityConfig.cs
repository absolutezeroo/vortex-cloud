namespace Vortex.Observability.Configuration;

/// <summary>
/// Strongly-typed options for the observability brick, bound from the <c>Vortex:Observability</c>
/// configuration section following the project's per-module options convention.
/// </summary>
public sealed class ObservabilityConfig
{
    public const string SECTION_NAME = "Vortex:Observability";

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
    /// Enables the native admin dashboard HTTP API. Off by default. Access is authenticated per
    /// account (email + password) and authorized by <c>dashboard.*</c> capabilities; there is no
    /// anonymous access and no shared token.
    /// </summary>
    public bool DashboardEnabled { get; init; }

    /// <summary>
    /// Serves the bundled SPA front-end (shell HTML + hashed JS/CSS assets) from the dashboard
    /// listener. On by default. Set to false to expose only the JSON API (for example when the
    /// front-end is hosted elsewhere or you want metrics/API without serving the SPA); asset and
    /// SPA-shell requests then return 404 while <c>/api/*</c> stays available.
    /// </summary>
    public bool DashboardFrontendEnabled { get; init; } = true;

    /// <summary>Host the dashboard binds to. Keep "localhost" unless a reverse proxy fronts it.</summary>
    public string DashboardHost { get; init; } = "localhost";

    /// <summary>TCP port for the dashboard HTTP API.</summary>
    public int DashboardPort { get; init; } = 9000;

    /// <summary>Lifetime of an authenticated dashboard session in minutes (minimum 5).</summary>
    public int DashboardSessionLifetimeMinutes { get; init; } = 480;

    /// <summary>When set, the dashboard also listens on HTTPS and redirects HTTP traffic to it.</summary>
    public bool DashboardHttpsEnabled { get; init; }

    /// <summary>TCP port for the dashboard HTTPS listener.</summary>
    public int DashboardHttpsPort { get; init; } = 9443;

    /// <summary>Optional PFX certificate used for the dashboard HTTPS listener; falls back to the dev certificate.</summary>
    public string? DashboardCertificatePath { get; init; }

    public string? DashboardCertificatePassword { get; init; }

    /// <summary>Emits HSTS headers for the dashboard (implies operators should only ever reach it over HTTPS).</summary>
    public bool DashboardHstsEnabled { get; init; }

    /// <summary>Fixed-window rate limit applied to dashboard <c>POST /api/login</c>.</summary>
    public DashboardRateLimitOptions DashboardLoginRateLimit { get; init; } =
        new DashboardRateLimitOptions
        {
            PermitLimit = 5,
            WindowSeconds = 60,
            QueueLimit = 0,
        };

    public sealed class DashboardRateLimitOptions
    {
        public int PermitLimit { get; init; } = 5;

        public int WindowSeconds { get; init; } = 60;

        public int QueueLimit { get; init; }
    }

    /// <summary>
    /// Optional URL template for furniture icons shown in the operations picker. Use <c>{name}</c>
    /// as the definition-name placeholder, for example
    /// <c>http://your-host/dcr/hof_furni/icons/{name}_icon.png</c>. Empty hides icons (a sprite-id
    /// tile is shown instead). When set, the icon host origin is added to the dashboard CSP
    /// <c>img-src</c> so the images are allowed to load.
    /// </summary>
    public string FurniIconUrlTemplate { get; init; } = string.Empty;

    /// <summary>
    /// Optional URL template for catalog page icons shown in the dashboard's catalog admin surface.
    /// Use <c>{id}</c> as the <c>CatalogPageEntity.Icon</c> placeholder, for example
    /// <c>http://vortex-assets.local/c_images/catalogue/icon_{id}.png</c>. Empty hides icons (a
    /// generic folder icon is shown instead). When set, the icon host origin is added to the
    /// dashboard CSP <c>img-src</c> so the images are allowed to load.
    /// </summary>
    public string CatalogIconUrlTemplate { get; init; } = string.Empty;

    /// <summary>
    /// Optional URL template for targeted-offer promo images shown in the dashboard's targeted-offer
    /// admin surface. Use <c>{file}</c> as the image filename placeholder, for example
    /// <c>http://vortex-assets.local/c_images/targetedoffers/{file}</c>. The admin then supplies just
    /// the filename instead of a full URL. Empty falls back to typing the whole URL. When set, the
    /// host origin is added to the dashboard CSP <c>img-src</c>.
    /// </summary>
    public string TargetedOfferImageUrlTemplate { get; init; } = string.Empty;

    /// <summary>
    /// Optional URL template for avatar images (player inspector heads) rendered from a player's
    /// figure string. Use <c>{figure}</c> as the placeholder, for example the Habbo imaging endpoint
    /// <c>http://vortex-assets.local/habbo-imaging/avatarimage?figure={figure}&amp;headonly=1&amp;size=m</c>.
    /// Empty hides the avatar (a generic icon is shown). When set, the host origin is added to the
    /// dashboard CSP <c>img-src</c>.
    /// </summary>
    public string AvatarImageUrlTemplate { get; init; } = string.Empty;

    /// <summary>
    /// Optional URL template for guild/group badges rendered from a group's badge code. Use
    /// <c>{badge}</c> as the placeholder, for example
    /// <c>http://vortex-assets.local/c_images/Badgeparts/generated/{badge}.gif</c>. Empty hides the
    /// badge (a generic icon is shown). When set, the host origin is added to the dashboard CSP
    /// <c>img-src</c>.
    /// </summary>
    public string GroupBadgeUrlTemplate { get; init; } = string.Empty;

    /// <summary>
    /// Optional local filesystem root of the asset host (e.g. <c>C:\Laragon\www\vortex-assets</c>),
    /// used ONLY to enumerate available images for dashboard pickers (e.g. the targeted-offer promo
    /// image gallery, so operators pick a real image instead of typing a filename blind). Empty
    /// disables the pickers and operators fall back to typing. Never used to serve files.
    /// </summary>
    public string AssetsLocalRoot { get; init; } = string.Empty;

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

    /// <summary>Rolling window in days for the top error groups query in an incident snapshot.</summary>
    public int IncidentErrorGroupWindowDays { get; init; } = 7;
}
