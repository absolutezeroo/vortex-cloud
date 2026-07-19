using Vortex.Primitives.Rooms.Enums.Wired;

namespace Vortex.Rooms.Wired.Logs;

public sealed record RoomWiredLogEntry
{
    public required int RoomId { get; init; }
    public required WiredLogLevel LogLevel { get; init; }
    public required WiredLogSource LogSource { get; init; }
    public required string Message { get; init; }
}
