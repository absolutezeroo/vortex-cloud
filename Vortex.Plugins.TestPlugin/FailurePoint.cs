using System;

namespace Vortex.Plugins.TestPlugin;

/// <summary>
/// Where <see cref="TestPlugin" /> should throw during activation.
/// <para>
/// Selected through an environment variable rather than a static field or a constructor argument:
/// the plugin is byte-loaded into its own collectible <c>AssemblyLoadContext</c>, so the test host
/// and the plugin get separate copies of every static. An environment variable is process-wide and
/// is the one channel both sides genuinely share.
/// </para>
/// </summary>
public enum FailurePoint
{
    None = 0,
    BindExports = 1,
    HostedServiceStart = 2,
    PluginStart = 3,
    Migration = 4,
}

/// <summary>Reads and writes the selected <see cref="FailurePoint" />.</summary>
public static class FailureSwitch
{
    public const string ENVIRONMENT_VARIABLE = "VORTEX_TEST_PLUGIN_FAILURE";

    /// <summary>Records what the plugin actually did, so the test can assert on it.</summary>
    public const string TRACE_VARIABLE = "VORTEX_TEST_PLUGIN_TRACE";

    public static FailurePoint Current =>
        Enum.TryParse(
            Environment.GetEnvironmentVariable(ENVIRONMENT_VARIABLE),
            ignoreCase: true,
            out FailurePoint point
        )
            ? point
            : FailurePoint.None;

    /// <summary>Appends a step name to the shared trace.</summary>
    public static void Trace(string step)
    {
        string existing = Environment.GetEnvironmentVariable(TRACE_VARIABLE) ?? string.Empty;

        Environment.SetEnvironmentVariable(
            TRACE_VARIABLE,
            existing.Length == 0 ? step : existing + "," + step
        );
    }
}
