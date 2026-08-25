namespace Vortex.Primitives.Observability;

/// <summary>
/// Why a wired chain stopped short of running everything it could have. These are the tag values of
/// <c>Vortex.wired.chain.stopped</c> — a small, closed set, because a metric tag with unbounded
/// values is a memory leak in the exporter.
/// <para>
/// Wired debugging has always been done through the room's own wired log, which is the right tool
/// in-game and useless across a hotel: nothing could answer "how often does anything actually hit
/// the depth limit". That question is what decides whether the limit should be 8 or 20 (OQ-1), and
/// it needs a counter rather than an opinion.
/// </para>
/// </summary>
public static class WiredStopReason
{
    /// <summary>The call chain reached <c>WiredMaxDepth</c>. The one OQ-1 is waiting on.</summary>
    public const string DEPTH = "depth";

    /// <summary>A pile tried to enter a tile already in the chain — a cycle, refused.</summary>
    public const string CYCLE = "cycle";

    /// <summary>The room event queue was full and the event was refused (WiredMaxQueuedEvents).</summary>
    public const string QUEUE_DROP = "queue-drop";

    /// <summary>The pile fired more often than its allowance window permits.</summary>
    public const string EXECUTION_LIMIT = "execution-limit";
}
