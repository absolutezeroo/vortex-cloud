using System.Diagnostics;

namespace Turbo.Observability.Diagnostics;

/// <summary>
/// Central names and the shared <see cref="ActivitySource"/> for the observability stack. External
/// exporters (OpenTelemetry, Prometheus, Seq, ...) subscribe to these names later without any code
/// change here: <c>AddSource("Turbo")</c> for traces and the <c>"Turbo"</c> meter for metrics.
/// </summary>
public static class TurboTelemetry
{
    public const string Name = "Turbo";
    public const string Version = "1.0.0";

    public static readonly ActivitySource ActivitySource = new(Name, Version);
}
