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
}
