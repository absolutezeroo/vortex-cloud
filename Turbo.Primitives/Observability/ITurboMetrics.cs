namespace Turbo.Primitives.Observability;

/// <summary>
/// Small façade over the runtime metric instruments. Kept intentionally narrow for the first
/// observability brick (packet pipeline only); further instruments are added as later phases land.
/// Implementations must be cheap and allocation-free on the hot path.
/// </summary>
public interface ITurboMetrics
{
    void PacketReceived(string operation);

    void PacketCompleted(string operation, double elapsedMilliseconds);

    void PacketFailed(string operation);
}
