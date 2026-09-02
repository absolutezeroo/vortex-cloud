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

    /// <summary>
    /// The room already held <c>WiredMaxPendingExecutions</c> delayed chains and another was
    /// refused. Distinct from <see cref="QUEUE_DROP"/> on purpose: that one says the room is being
    /// told about more than it can read, this one says the room's own wired is arranging more
    /// future work than it gets through — which is a room to go and look at, not a busy hotel.
    /// </summary>
    public const string SCHEDULE_DROP = "schedule-drop";

    /// <summary>The pile fired more often than its allowance window permits.</summary>
    public const string EXECUTION_LIMIT = "execution-limit";

    /// <summary>
    /// An action's effective selection held more players than <c>WiredSelectedPlayersLimit</c> and
    /// the rest were dropped. The only one of these a player can raise by walking into a room, which
    /// is why it is a counter rather than a log line: a room hitting it repeatedly is either an
    /// operator running a bigger hotel than the limit assumes, or the abuse the limit is there for.
    /// </summary>
    public const string SELECTION_LIMIT = "selection-limit";

    /// <summary>
    /// A delayed effect lost its pile during the wait — dragged onto another tile, or picked up —
    /// and was refused at execution time. Habbo only lets a trigger drive the boxes stacked with it,
    /// and a delay is the one window in which that can stop being true after the pile was resolved.
    /// <para>
    /// The quietest of the five. The other four are refusals of something the room asked for; this
    /// one is a box that was going to fire, and then did not, with nothing anywhere to say so — which
    /// is exactly the report that arrives as "my wired stopped working".
    /// </para>
    /// </summary>
    public const string REVALIDATION = "revalidation";
}

/// <summary>
/// What became of a room event the wired engine was handed. The tag values of
/// <c>Vortex.wired.event</c>.
/// </summary>
/// <remarks>
/// There is no <c>received</c>: it is the sum of these two and the <c>queue-drop</c> chain stops,
/// and an extra increment per event on a path that runs at the room tick rate buys nothing a query
/// cannot add up. What the ratio answers is whether the index short-circuit is doing its job — a
/// hotel where nearly everything is processed rather than ignored has rooms full of triggers, and
/// one where nothing is ignored has an index that is permanently dirty.
/// </remarks>
public static class WiredEventOutcome
{
    /// <summary>No trigger in the room listens for this event type, or none is left to consume it.</summary>
    public const string IGNORED = "ignored";

    /// <summary>Dequeued and offered to the triggers that listen for it.</summary>
    public const string PROCESSED = "processed";
}
