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
    /// A value-moving operation changed state. Before this there was no signal at all for the
    /// commerce flows — an operation stuck past its pivot was indistinguishable from one that
    /// completed, for as long as nobody happened to look at the table.
    /// </summary>
    void CommerceOperationTransitioned(
        Vortex.Primitives.Commerce.CommerceOperationKind kind,
        Vortex.Primitives.Commerce.CommerceOperationState state
    );

    /// <summary>
    /// A post-pivot step was asked to run again and found its own receipt. Expected traffic on a
    /// retry; a rising rate means something upstream is failing after doing its work.
    /// </summary>
    void CommerceStepReplayed(string stepKey);

    /// <summary>
    /// A wired chain stopped short of running everything it could have.
    /// <paramref name="reason"/> is one of <see cref="WiredStopReason"/> — a closed set, so it is
    /// safe as a tag. Nothing before this could answer how often a room actually hits its depth
    /// limit, which is the only evidence that settles what the limit should be.
    /// </summary>
    void WiredChainStopped(string reason);

    /// <summary>
    /// A call to the (single, global) room directory grain returned. <paramref name="method"/> is the
    /// grain method name, which is bounded by the interface.
    /// </summary>
    void RoomDirectoryCallCompleted(string method, double elapsedMilliseconds);
}
