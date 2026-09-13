namespace Vortex.Database.Configuration;

public class DatabaseConfig
{
    public const string SECTION_NAME = "Vortex:Database";

    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>
    /// Explicit MySQL server version (e.g. "8.0.36-mysql"), so <c>ServerVersion.Parse</c> can be used
    /// instead of <c>ServerVersion.AutoDetect</c>, which opens a connection during DI configuration
    /// itself. Left unset, the driver falls back to auto-detection.
    /// </summary>
    public string? MySqlServerVersion { get; init; }

    /// <summary>
    /// Applies pending migrations during startup, before any listener opens. Off by default: two
    /// hosts racing the same migration is a real hazard, so this is a single-node convenience and
    /// not a deployment default. On a single node it is the difference between a redeploy that
    /// works and one that answers 500 on every login because the schema step was skipped.
    /// </summary>
    public bool MigrateOnStartup { get; init; }
}
