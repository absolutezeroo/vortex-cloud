using System.Diagnostics;

namespace Vortex.Observability.Diagnostics;

/// <summary>
/// Central names and the shared <see cref="ActivitySource"/> for the observability stack. External
/// exporters (OpenTelemetry, Prometheus, Seq, ...) subscribe to these names later without any code
/// change here: <c>AddSource("Vortex")</c> for traces and the <c>"Vortex"</c> meter for metrics.
/// </summary>
public static class VortexTelemetry
{
    public const string Name = "Vortex";
    public const string Version = "1.0.0";

    public static readonly ActivitySource ActivitySource = new(Name, Version);
}
