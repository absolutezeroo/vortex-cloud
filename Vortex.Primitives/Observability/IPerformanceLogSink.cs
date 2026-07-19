namespace Vortex.Primitives.Observability;

/// <summary>
/// Non-blocking client performance telemetry sink for tracking packets.
/// Implementations must enqueue and return quickly; durable persistence happens on a
/// background path.
/// </summary>
public interface IPerformanceLogSink
{
    void Record(in PerformanceLogEvent entry);
}
