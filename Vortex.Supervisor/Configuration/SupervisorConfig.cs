namespace Vortex.Supervisor.Configuration;

/// <summary>
///     The supervisor's own listener and the child process it owns, bound from
///     <c>Vortex:Supervisor</c>. This process exists to outlive the emulator, so none of it can be
///     read from the emulator's running state.
/// </summary>
public sealed class SupervisorConfig
{
    public const string SECTION_NAME = "Vortex:Supervisor";

    /// <summary>Bind address for the control listener. Loopback unless deliberately widened.</summary>
    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 5250;

    /// <summary>
    ///     The shared secret every control request must present as <c>Authorization: Bearer</c>.
    ///     There is no login here on purpose: the supervisor must answer while the emulator (and
    ///     therefore the database holding staff accounts) is down, so it cannot authenticate against
    ///     it. Startup refuses the shipped placeholder.
    /// </summary>
    public string Token { get; init; } = PLACEHOLDER_TOKEN;

    public const string PLACEHOLDER_TOKEN =
        "CHANGE_ME__set-via-VORTEX__Vortex__Supervisor__Token-env-var-or-user-secrets";

    /// <summary>
    ///     Explicit opt-in to serving the control surface off-box over plain HTTP. The token and the
    ///     console stream both travel in cleartext when this is on.
    /// </summary>
    public bool AllowInsecureRemoteHttp { get; init; }

    /// <summary>How many console lines to retain for replay to a newly attached viewer.</summary>
    public int ConsoleBufferLines { get; init; } = 2000;

    /// <summary>
    ///     Absolute or relative URL of the emulator's <c>/health</c> endpoint. Polled to tell
    ///     "the process is alive" apart from "the hotel is actually serving" — a process that is up
    ///     but cannot reach the database is not a running hotel.
    /// </summary>
    public string HealthUrl { get; init; } = "http://localhost:8080/health";

    public int HealthPollSeconds { get; init; } = 5;

    public EmulatorProcessConfig Emulator { get; init; } = new();
}

public sealed class EmulatorProcessConfig
{
    public string ExecutablePath { get; init; } = "dotnet";

    public string Arguments { get; init; } = "Vortex.Main.dll";

    /// <summary>
    ///     Resolved to an absolute path before launching. A relative value resolves against the
    ///     supervisor's own directory rather than whatever directory it happened to be started from,
    ///     so the emulator cannot silently pick up a different appsettings.json than intended.
    /// </summary>
    public string WorkingDirectory { get; init; } = ".";

    /// <summary>
    ///     Written to the child's stdin to ask for a graceful shutdown. This reaches
    ///     <c>ConsoleCommandService</c>, whose <c>quit</c> stops the host properly.
    /// </summary>
    public string GracefulShutdownCommand { get; init; } = "quit";

    /// <summary>
    ///     How long to wait for the child to exit on its own before killing it. Room state and
    ///     pending writes flush during that window, so this is deliberately generous.
    /// </summary>
    public int GracefulShutdownTimeoutSeconds { get; init; } = 30;
}
