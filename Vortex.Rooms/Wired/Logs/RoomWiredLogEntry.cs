using Vortex.Primitives.Rooms.Enums.Wired;

namespace Vortex.Rooms.Wired.Logs;

public sealed record RoomWiredLogEntry
{
    public required int RoomId { get; init; }
    public required WiredLogLevel LogLevel { get; init; }
    public required WiredLogSource LogSource { get; init; }
    public required string Message { get; init; }

    /// <summary>
    /// The execute-stacks chain step this line came from, or 0 when it came from outside one.
    /// </summary>
    /// <remarks>
    /// The log is written by every pile in the room, interleaved, and until now a reader had the
    /// order and nothing else — two chains firing in the same tick produced one list nobody could
    /// separate. Filtering on a non-zero id gives one chain, and <see cref="ParentExecutionId"/>
    /// gives the one that called it.
    /// </remarks>
    public int ExecutionId { get; init; }

    /// <summary>The chain step that called this one, or 0 when this one started the chain.</summary>
    public int ParentExecutionId { get; init; }
}
