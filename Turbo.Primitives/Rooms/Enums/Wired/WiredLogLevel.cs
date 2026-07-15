namespace Turbo.Primitives.Rooms.Enums.Wired;

/// <summary>Best-effort values, not verified against a real WIN63 client capture — see the plan's
/// residual-risk notes for the wired room-logs feature.</summary>
public enum WiredLogLevel : byte
{
    Info = 0,
    Warning = 1,
    Error = 2,
}
