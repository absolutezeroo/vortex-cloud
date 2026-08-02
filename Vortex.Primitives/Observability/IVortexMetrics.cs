namespace Vortex.Primitives.Observability;

/// <summary>
/// Small façade over the runtime metric instruments. Kept intentionally narrow; further instruments
/// are added as later phases land. Implementations must be cheap and allocation-free on the hot path.
/// Every recording method takes only bounded dimensions — a player id or a room id must never reach a
/// metric tag; those breakdowns belong to <c>ILiveStatsAggregator</c>.
/// </summary>
public interface IVortexMetrics
{
    /// <summary>
    /// Whether anything is actually recorded. Callers on a hot path (the room tick runs every 50ms
    /// per room) read this first so that a disabled metrics stack costs one boolean test instead of a
    /// timestamp pair per step.
    /// </summary>
    bool Enabled { get; }

    void PacketReceived(string operation, long? actorId = null, int? roomId = null);

    void PacketCompleted(
        string operation,
        double elapsedMilliseconds,
        long? actorId = null,
        int? roomId = null
    );

    void PacketFailed(string operation, long? actorId = null, int? roomId = null);

    void PacketDropped(string reason);

    /// <summary>
    /// One step of a room tick finished. <paramref name="step"/> is the fixed step name declared in
    /// the tick loop ("pets", "wired", ...) — a bounded dimension, so it is safe as a tag.
    /// </summary>
    void RoomTickStepCompleted(string step, double elapsedMilliseconds);

    /// <summary>A whole room tick finished, all steps included. Untagged: the room id is not safe to tag by.</summary>
    void RoomTickCompleted(double elapsedMilliseconds);

    /// <summary>
    /// A call to the (single, global) room directory grain returned. <paramref name="method"/> is the
    /// grain method name, which is bounded by the interface.
    /// </summary>
    void RoomDirectoryCallCompleted(string method, double elapsedMilliseconds);
}
