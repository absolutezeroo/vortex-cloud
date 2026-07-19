namespace Vortex.Database.Configuration;

public class DatabaseConfig
{
    public const string SECTION_NAME = "Vortex:Database";

    public string ConnectionString { get; init; } = string.Empty;
    public bool LoggingEnabled { get; init; } = false;
}
