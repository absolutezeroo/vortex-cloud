using Turbo.Primitives.Rooms.Enums.Wired;

namespace Turbo.Rooms.Wired.Logs;

public sealed record RoomWiredLogEntry
{
    public required int RoomId { get; init; }
    public required WiredLogLevel LogLevel { get; init; }
    public required WiredLogSource LogSource { get; init; }
    public required string Message { get; init; }
}
